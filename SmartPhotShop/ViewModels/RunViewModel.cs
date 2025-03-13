using AutoMapper;
using Caliburn.Micro;
using DocumentFormat.OpenXml.Vml;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using Photoshop;
using SmartPhotShop.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using LogManager = NLog.LogManager;

namespace SmartPhotShop.ViewModels
{
    public class BusyIndicator : IResult
    {
        private readonly bool _hidden;


        public event EventHandler<ResultCompletionEventArgs> Completed;

        public BusyIndicator(bool hide)
        {
            _hidden = hide;
        }

        public void Execute(CoroutineExecutionContext context)
        {
            var view = context.View as FrameworkElement;


            if (view == null)
            {
                Completed(this, new ResultCompletionEventArgs());
                return;
            }

            // Search downward for ProgressBar
            var busyIndicator = view.FindName("IsBusyIndicator") as StackPanel;

            if (busyIndicator != null)
            {
                busyIndicator.Visibility = _hidden ? Visibility.Collapsed : Visibility.Visible;
            }

            Completed(this, new ResultCompletionEventArgs());
        }

        // Recursive method to search down the visual tree
        private System.Windows.Controls.ProgressBar FindProgressBarDownward(FrameworkElement element)
        {
            if (element == null)
                return null;

            foreach (var child in LogicalTreeHelper.GetChildren(element))
            {
                if (child is System.Windows.Controls.ProgressBar progressBar)
                {
                    return progressBar;
                }
                if (child is FrameworkElement childElement)
                {
                    var found = FindProgressBarDownward(childElement);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        public static BusyIndicator Show()
        {
            return new BusyIndicator(false);
        }

        public static BusyIndicator Hide()
        {
            return new BusyIndicator(true);
        }
    }
    class RunViewModel : Caliburn.Micro.Screen
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private string baseImage;
        private volatile bool continueRunning = false;

        private BackgroundWorker bgWorker;
        private string actionSet;
        private string actionName;
        private readonly ConcurrentQueue<string> _filesQueue = new ConcurrentQueue<string>();
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;
        private string _workingDirectory;

        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { Set(ref _workingDirectory, value); }
        }

        public string BaseImage { get => baseImage; set => Set(ref baseImage, value); }
        public string ActionSet { get => actionSet; set => Set(ref actionSet, value); }
        public string ActionName { get => actionName; set => Set(ref actionName, value); }
        public BindableCollection<ProcessItem> Items { get; set; } = new BindableCollection<ProcessItem>();
        public RunViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Run";
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            OnUIThread(() =>
            {
                _mapper.Map(Properties.Settings.Default, this);
            });

            return base.OnActivateAsync(cancellationToken);
        }

        public void BrowseBaseImage()
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Base Image"
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            BaseImage = dialog.FileName;

        }

        public IEnumerable<IResult> Start()
        {
            if (CanRun() == false)
            {
                yield return _dialogCoordinator.ShowMessageAsync(this, "Error", "Please fill in all the required fields\nYou might be missing some Settings!").AsResult();
                yield break;
            }

            WorkingDirectory = Properties.Settings.Default.WorkingDirectory;

            yield return BusyIndicator.Show();

            _mapper.Map(this, Properties.Settings.Default);
            Properties.Settings.Default.Save();

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerAsync();

            continueRunning = true;

            NotifyOfPropertyChange(nameof(CanStart));
            NotifyOfPropertyChange(nameof(CanStop));
        }

        private bool CanRun()
        {
            return !string.IsNullOrEmpty(BaseImage) && !string.IsNullOrEmpty(ActionName) && !string.IsNullOrEmpty(ActionSet)
                && !string.IsNullOrEmpty(Properties.Settings.Default.FlatFile) && !string.IsNullOrEmpty(Properties.Settings.Default.WorkingDirectory);
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var fileWatcher = new FileSystemWatcher(Properties.Settings.Default.WorkingDirectory, "*.*")
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                fileWatcher.Created += Fs_Created;

                AutoResetEvent fileEvent = new AutoResetEvent(false);
                Photoshop.Application photoshop = null;

                while (continueRunning)
                {
                    if (_filesQueue.TryDequeue(out var item))
                    {
                        if (IsFileReady(item))
                        {
                            var uiItem = Items.FirstOrDefault(i => i.OriginalFileName == item);
                            if (uiItem != null)
                            {
                                OnUIThread(() => uiItem.Status = "Processing");

                                if (photoshop == null)
                                {
                                    photoshop = new Photoshop.Application { Visible = true };
                                }
                                ProcessImage(photoshop, item);
                            }
                        }
                        else
                        {
                            _filesQueue.Enqueue(item);
                        }
                    }
                    else
                    {
                        fileEvent.WaitOne(100); // Avoid CPU-intensive loop
                    }
                }

                photoshop?.Quit();
                fileWatcher.Created -= Fs_Created;
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
            finally
            {
                continueRunning = false;
                NotifyOfPropertyChange(nameof(CanStart));
                NotifyOfPropertyChange(nameof(CanStop));
            }
        }

        private bool MoveFile(string source, string dest)
        {
            try
            {
                // Move the original image to the Done directory
                File.Copy(source, dest, true);
                File.Delete(source);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Unable to move file from '{source}' to '{dest}'");
                logger.Error(ex.Message, ex);
                return false;
            }
        }


        private void ProcessImage(Photoshop.Application photoshop, string imageItemPath)
        {
            var uiItem = Items.FirstOrDefault(i => i.OriginalFileName == imageItemPath);
            string baseImagePath = BaseImage;
            string actionName = ActionName;
            string actionSet = ActionSet;
            string outputDirectory = Properties.Settings.Default.OutputDirectory;
            string doneDirectory = Properties.Settings.Default.DoneDirectory;
            string errorDirectory = Properties.Settings.Default.ErrorDirectory;

            Document baseImageDoc = null;
            Document imageDoc = null;

            try
            {
                // Open the base image if necessary
                if (!string.IsNullOrEmpty(baseImagePath) && File.Exists(baseImagePath))
                {
                    baseImageDoc = photoshop.Open(baseImagePath);
                }

                // Open the image to process
                imageDoc = photoshop.Open(imageItemPath);

                // Perform the action
                photoshop.DoAction(actionName, actionSet);

                // Define the file path to save the PNG
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(imageItemPath);
                string pngFileName = $"{fileNameWithoutExtension}.png";
                string pngFilePath = System.IO.Path.Combine(outputDirectory, pngFileName);

                // Create an instance of PNG save options
                PNGSaveOptions pngOptions = new PNGSaveOptions();

                // Save the active document as PNG
                imageDoc.SaveAs(pngFilePath, pngOptions, true);

                // Define the destination path in the Done directory
                string destFileName = System.IO.Path.GetFileName(imageItemPath);
                string destPath = System.IO.Path.Combine(doneDirectory, destFileName);

                MoveFile(imageItemPath, destPath);

                // Update UI item status
                if (uiItem != null)
                {
                    OnUIThread(() =>
                    {
                        uiItem.Status = "Success";
                        uiItem.MovedFileName = destPath;
                    });
                }
            }
            catch (Exception ex)
            {
                // Define the destination path in the Error directory
                string errorFileName = System.IO.Path.GetFileName(imageItemPath);
                string errorPath = System.IO.Path.Combine(errorDirectory, errorFileName);

                // Move the original image to the Error directory
                MoveFile(imageItemPath, errorPath);

                // Update UI item status
                if (uiItem != null)
                {
                    OnUIThread(() =>
                    {
                        uiItem.Status = "Error";
                        uiItem.MovedFileName = errorPath;
                    });
                }

                // Log the original error
                logger.Error($"Error processing image '{imageItemPath}': {ex.Message}");
            }
            finally
            {
                // Close the documents to free up resources
                baseImageDoc?.Close(2);
                imageDoc?.Close(2);
            }
        }

        public static bool IsFileReady(string filePath)
        {
            try
            {
                // Attempt to open the file exclusively
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If successful, the file is ready for reading
                }
            }
            catch (IOException)
            {
                // If an IOException is caught, the file is still in use
                return false;
            }

            // No exception means the file is ready
            return true;
        }
        private void Fs_Created(object sender, FileSystemEventArgs e)
        {
            var ext = System.IO.Path.GetExtension(e.FullPath)?.ToLower();
            var supportedExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif", ".webp", ".heic" };

            if (string.IsNullOrEmpty(ext) || !supportedExtensions.Contains(ext))
                return;


            var processItem = new ProcessItem
            {
                OriginalFileName = e.FullPath,
                DateAdded = DateTime.Now,
                Status = "Pending"
            };

            OnUIThread(() => Items.Add(processItem));

            _filesQueue.Enqueue(e.FullPath);
        }

        public IEnumerable<IResult> Stop()
        {
            yield return BusyIndicator.Hide();

            continueRunning = false;

            NotifyOfPropertyChange(nameof(CanStart));
            NotifyOfPropertyChange(nameof(CanStop));
        }

        public bool CanStart => !continueRunning;
        public bool CanStop => continueRunning;
    }
}
