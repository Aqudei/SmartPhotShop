using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using AutoMapper;
using Caliburn.Micro;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Vml;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using Photoshop;
using SmartPhotShop.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml.Linq;
using Application = Photoshop.Application;
using LogManager = NLog.LogManager;
using Path = System.IO.Path;

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
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ILiteDatabase _db;
        private string _workingDirectory;

        private HashSet<string> _supportedFiles = new HashSet<string> { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif", ".webp", ".heic" };
        private List<ProductTemplate> _products;

        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { Set(ref _workingDirectory, value); }
        }

        public BindableCollection<ProcessingItem> Items { get; set; } = new BindableCollection<ProcessingItem>();
        public RunViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator, ILiteDatabase db)
        {
            DisplayName = "Run";
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
            _db = db;
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            return base.OnActivateAsync(cancellationToken);
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

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerAsync();

            continueRunning = true;

            NotifyOfPropertyChange(nameof(CanStart));
            NotifyOfPropertyChange(nameof(CanStop));
        }

        private bool CanRun()
        {
            return !string.IsNullOrEmpty(Properties.Settings.Default.FlatFile) && !string.IsNullOrEmpty(Properties.Settings.Default.WorkingDirectory);
        }


        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var fields = _db.GetCollection<Field>().FindAll().AsQueryable();
            var productItemCollection = _db.GetCollection<ProductItem>();

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
                    var processingItem = Items.FirstOrDefault(i => i.Status == "Pending");

                    if (processingItem != null)
                    {
                        try
                        {
                            WaitUntilFileIsReady(processingItem.Overlay);

                            OnUIThread(() => processingItem.Status = "Processing");


                            if (photoshop == null)
                            {
                                photoshop = new Photoshop.Application { Visible = true };

                            }

                            var productImages = new List<ProductImage>();

                            foreach (var baseImage in processingItem.ProductTemplate.Images)
                            {
                                try
                                {
                                    var outputFileName = $"{processingItem.ProductTemplate.Name} {baseImage.Name} {Path.GetFileNameWithoutExtension(processingItem.Overlay)}";
                                    outputFileName = Regex.Replace(outputFileName, @"\s+", " ");
                                    outputFileName = Path.ChangeExtension(outputFileName, ".jpg");

                                    var outputFilePath = System.IO.Path.Combine(Properties.Settings.Default.OutputDirectory, processingItem.ProductTemplate.Name, outputFileName);
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                                    ProcessImage(photoshop, processingItem, baseImage.Path, outputFilePath);
                                    productImages.Add(new ProductImage
                                    {
                                        Name = baseImage.Name,
                                        Path = outputFilePath
                                    });
                                }
                                catch (Exception ex)
                                {
                                    logger.Error($"Error processing image '{baseImage.Name}' for SKU '{processingItem.ProductTemplate.SKU}'");
                                    logger.Error(ex, ex.Message);
                                }
                            }

                            var mainImage = productImages.FirstOrDefault(x => x.Name.ToLower().Contains("main")) ?? productImages.First();

                            var productItem = productItemCollection.FindOne(x => x.SKU == processingItem.Sku) ?? new ProductItem();
                            _mapper.Map(processingItem.ProductTemplate, productItem);

                            productItem.SKU = processingItem.Sku;
                            productItem.ProductTemplateId = processingItem.ProductTemplate.Id;

                            productItem.Images.Clear();
                            productItem.Images.AddRange(productImages);

                            productItem.FieldValues.Clear();
                            productItem.FieldValues.AddRange(processingItem.ProductTemplate.FieldValues);

                            productItem.SetFieldValues(fields, "SKU", processingItem.Sku);

                            var itemName = $"{processingItem.ProductTemplate.Name} {Path.GetFileNameWithoutExtension(processingItem.Overlay)}";

                            productItem.SetFieldValues(fields, "Item Name", itemName);

                            var key = $"{processingItem.ProductTemplate.Name}/{processingItem.ProductTemplate.SKU} {mainImage.Name} {Path.GetFileNameWithoutExtension(processingItem.Overlay)}".ToLower();
                            key = Path.ChangeExtension(Regex.Replace(key.Replace("-", " "), @"\s+", "-"), ".jpg");

                            productItem.SetFieldValues(fields, "Main Image URL", $"https://{Constants.BucketName}.s3.eu-north-1.amazonaws.com/{key}");
                            var otherImagesUrls = productImages.Where(p => !p.Equals(mainImage))
                                .Select(p =>
                                {
                                    var k = $"{processingItem.ProductTemplate.Name}/{processingItem.ProductTemplate.SKU} {p.Name} {Path.GetFileNameWithoutExtension(processingItem.Overlay)}".ToLower();
                                    k = Path.ChangeExtension(Regex.Replace(k.Replace("-", " "), @"\s+", "-"), ".jpg");
                                    return $"https://{Constants.BucketName}.s3.eu-north-1.amazonaws.com/{k}";
                                });
                            productItem.SetFieldValues(fields, "Other Image URL", otherImagesUrls.ToArray());


                            // "thesoleengraver.s3.eu-north-1.amazonaws.com"
                            if (productItem.Id == 0)
                            {
                                productItemCollection.Insert(productItem);
                            }
                            else
                            {
                                productItemCollection.Update(productItem);
                            }

                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, ex.Message);
                        }
                        finally
                        {

                            // MoveFile(uiItem, Properties.Settings.Default.DoneDirectory);
                            OnUIThread(() => processingItem.Status = "Done");
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

        //private async Task UploadToS3(ProductItem productItem)
        //{
        //    var bucketName = "thesoleengraver";
        //    var accessId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        //    var accessSecret = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        //    using (var s3Client = new AmazonS3Client(accessId, accessSecret, RegionEndpoint.SAEast1))
        //    {
        //        var fileTransferUtility = new TransferUtility(s3Client);
        //        foreach (var image in productItem.Images)
        //        {

        //        }
        //    }

        //}

        private bool MoveFile(string source, string destination)
        {
            try
            {
                // Move the original image to the Done directory
                File.Copy(source, destination, true);
                File.Delete(source);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Unable to move file from '{source}' to '{destination}'");
                logger.Error(ex.Message, ex);
                return false;
            }
        }


        private void ProcessImage(Photoshop.Application photoshop, ProcessingItem uiItem, string baseImagePath, string outputFilePath)
        {
            var actionSet = Properties.Settings.Default.ActionSet;
            var outputDirectory = Properties.Settings.Default.OutputDirectory;
            var doneDirectory = Properties.Settings.Default.DoneDirectory;
            var errorDirectory = Properties.Settings.Default.ErrorDirectory;
            var productsDirectory = Properties.Settings.Default.ProductsDirectory;

            var actionName = Path.GetFileNameWithoutExtension(baseImagePath);

            Debug.WriteLine($"Running ATN: {actionSet}::{actionName}");

            Document baseImageDoc = null;
            Document imageDoc = null;

            try
            {

                baseImageDoc = photoshop.Open(baseImagePath);

                // Open the image to process
                imageDoc = photoshop.Open(uiItem.Overlay);

                // Perform the action
                photoshop.DoAction(actionName, actionSet);


                // Create an instance of PNG save options
                JPEGSaveOptions pngOptions = new JPEGSaveOptions();

                // Save the active document as PNG
                imageDoc.SaveAs(outputFilePath, pngOptions, true);
            }
            catch (Exception ex)
            {
                logger.Error($"Error processing image '{uiItem}' using Action: {actionName}: {ex.Message}");
            }
            finally
            {
                // Close the documents to free up resources
                baseImageDoc?.Close(2);
                imageDoc?.Close(2);
            }
        }
        public static void WaitUntilFileIsReady(string filePath, int retryIntervalMs = 500, int timeoutMs = 10000)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (IsFileReady(filePath))
                    return; // File is ready

                Thread.Sleep(retryIntervalMs); // Wait before retrying
            }

            throw new TimeoutException($"Timeout waiting for file {filePath} to become available.");
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

            if (string.IsNullOrEmpty(ext) || !_supportedFiles.Contains(ext))
                return;

            var productTemplates = _db.GetCollection<ProductTemplate>().FindAll().ToList();
            if (!productTemplates.Any())
                return;

            foreach (var productTemplate in productTemplates)
            {
                var processItem = new ProcessingItem(e.FullPath, productTemplate);
                OnUIThread(() => Items.Add(processItem));
            }
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
