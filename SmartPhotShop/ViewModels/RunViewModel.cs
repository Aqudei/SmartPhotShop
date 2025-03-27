using AutoMapper;
using Caliburn.Micro;
using DocumentFormat.OpenXml.Vml;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using NLog;
using OfficeOpenXml;
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

        public string DbPath { get; private set; }

        private string _workingDirectory;

        private HashSet<string> _supportedFiles = new HashSet<string> { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif", ".webp", ".heic" };
        private List<ProductInfo> _products;

        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set { Set(ref _workingDirectory, value); }
        }

        public BindableCollection<ProcessItem> Items { get; set; } = new BindableCollection<ProcessItem>();
        public RunViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Run";
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;

            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
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


        static void UpdateOrInsertRow(ExcelPackage excel, string filePath, string sheetName, string sku, string[] newData)
        {
            var worksheet = excel.Workbook.Worksheets[sheetName];
            if (worksheet == null)
            {
                Console.WriteLine($"Sheet '{sheetName}' not found.");
                return;
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            bool found = false;

            // Search for SKU in Column A (Column 1)
            for (int row = 2; row <= rowCount; row++) // Skipping header row
            {
                if (worksheet.Cells[row, 1].Text.Equals(sku, StringComparison.OrdinalIgnoreCase))
                {
                    // SKU exists, update row
                    for (int i = 0; i < newData.Length; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = newData[i];
                    }
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // SKU not found, insert new row
                int newRow = rowCount + 1;
                for (int i = 0; i < newData.Length; i++)
                {
                    worksheet.Cells[newRow, i + 1].Value = newData[i];
                }
            }

            excel.Save();
            Debug.WriteLine($"Excel file <{filePath}> updated successfully.");
        }

        public void UpdateFlatFile()
        {
            using (var excel = new ExcelPackage(Properties.Settings.Default.FlatFile))
            using (var db = new LiteDatabase(DbPath))
            {
                var flatFile = Properties.Settings.Default.FlatFile;
                var sheetName = "Template";
                var products = db.GetCollection<OutputItem>().FindAll();

                foreach (var product in products)
                {
                    var data = new[] { product.Sku, "" };
                    UpdateOrInsertRow(excel, flatFile, sheetName, product.Sku, data);
                }
            }
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
                    var uiItem = Items.FirstOrDefault(i => i.Status == "Pending");

                    if (uiItem != null)
                    {
                        try
                        {
                            WaitUntilFileIsReady(uiItem.Overlay);

                            OnUIThread(() => uiItem.Status = "Processing");


                            if (photoshop == null)
                            {
                                photoshop = new Photoshop.Application { Visible = true };

                            }

                            var outputFileName = $"{uiItem.Variant.VariantName.ToUpper()}-WITH-{Path.GetFileNameWithoutExtension(uiItem.Overlay).ToUpper()}.png".Replace(" ", "-");
                            var outputFilePath = System.IO.Path.Combine(Properties.Settings.Default.OutputDirectory, uiItem.Product.ProductName, outputFileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

                            ProcessImage(photoshop, uiItem, outputFilePath);


                            using (var db = new LiteDatabase(DbPath))
                            {

                                var dbItem = db.GetCollection<OutputItem>().FindOne(x => x.Sku == uiItem.Sku);

                                int maxId = 0;


                                try
                                {
                                    maxId = db.GetCollection<OutputItem>().FindAll().Max(o => o.ProductId);
                                }
                                catch (Exception)
                                {
                                }

                                if (dbItem == null)
                                {
                                    var outputItem = new OutputItem
                                    {
                                        Sku = uiItem.Sku,
                                        ProductId = maxId + 1,
                                        Location = outputFilePath
                                    };

                                    var inserted = db.GetCollection<OutputItem>().Insert(outputItem);

                                    // var data = new[] { uiItem.Sku, outputItem.ProductId.ToString() };
                                    // UpdateOrInsertRow(Properties.Settings.Default.FlatFile, "Template", uiItem.Sku, data);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, ex.Message);
                        }
                        finally
                        {

                            // MoveFile(uiItem, Properties.Settings.Default.DoneDirectory);
                            OnUIThread(() => uiItem.Status = "Done");
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


        private void ProcessImage(Photoshop.Application photoshop, ProcessItem uiItem, string outputFilePath)
        {
            var actionSet = Properties.Settings.Default.ActionSet;
            var outputDirectory = Properties.Settings.Default.OutputDirectory;
            var doneDirectory = Properties.Settings.Default.DoneDirectory;
            var errorDirectory = Properties.Settings.Default.ErrorDirectory;
            var productsDirectory = Properties.Settings.Default.ProductsDirectory;

            var baseImagePath = uiItem.Variant.VariantPath;
            var actionName = uiItem.Variant.VariantName;

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
                PNGSaveOptions pngOptions = new PNGSaveOptions();

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

            var products = Directory.EnumerateDirectories(Properties.Settings.Default.ProductsDirectory)
                .Select(d => new ProductInfo(d))
                .ToList();

            foreach (var product in products)
            {
                foreach (var design in product.Variants)
                {
                    var processItem = new ProcessItem(e.FullPath, design, product);

                    OnUIThread(() => Items.Add(processItem));
                }

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
