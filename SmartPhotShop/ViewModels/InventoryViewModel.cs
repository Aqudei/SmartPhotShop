using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Caliburn.Micro;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using OfficeOpenXml;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    internal class InventoryViewModel : Screen
    {
        private const string BucketName = "thesoleengraver";
        //private string AWS_ACCESS_KEY_ID = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID", EnvironmentVariableTarget.User) ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID", EnvironmentVariableTarget.Machine);
        //private string AWS_SECRET_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        public BindableCollection<ProductImage> Images { get; set; } = new BindableCollection<ProductImage>();
        public BindableCollection<ProductItem> Items { get; set; } = new BindableCollection<ProductItem>();
        public ProductItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
        public InventoryViewModel(IDialogCoordinator dialogCoordinator, ILiteDatabase liteDatabase)
        {
            DisplayName = "Files";

            PropertyChanged += InventoryViewModel_PropertyChanged;
            _dialogCoordinator = dialogCoordinator;
            _db = liteDatabase;
        }
        static void UpdateOrInsertRow(ExcelPackage excel, string filePath, string sheetName, string sku, List<object> newData)
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
                    for (int i = 0; i < newData.Count; i++)
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
                for (int i = 0; i < newData.Count; i++)
                {
                    worksheet.Cells[newRow, i + 1].Value = newData[i];
                }
            }

            excel.Save();
            Debug.WriteLine($"Excel file <{filePath}> updated successfully.");
        }

        public async void UpdateFlatFile()
        {
            await Task.Run(UpdateFlatFileTask);
        }
        public async Task UpdateFlatFileTask()
        {
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Please wait", "Updating Flat File...");
            progress.SetIndeterminate();

            try
            {
                using (var excel = new ExcelPackage(Properties.Settings.Default.FlatFile))
                {
                    var productTemplatesCollection = _db.GetCollection<ProductTemplate>().FindAll();
                    var flatFile = Properties.Settings.Default.FlatFile;
                    var sheetName = "Template";
                    var productItems = _db.GetCollection<ProductItem>().FindAll().ToList();

                    foreach (var productItem in productItems)
                    {
                        var data = new List<object> { productItem.SKU };

                        if (productItem != null)
                        {
                            UpdateOrInsertRow(excel, flatFile, sheetName, (string)productItem.SKU, data);
                        }

                        // Process.Start(Properties.Settings.Default.FlatFile);

                    }
                }


                await UploadFlatFileAsync(Properties.Settings.Default.FlatFile);

                progress.SetMessage("Uploading output files to S3...");
                await SyncProductItems();

            }
            catch (Exception)
            { }
            finally
            {
                await progress.CloseAsync();
            }
        }

        private async Task SyncProductItems()
        {
            try
            {
                //using (var s3Client = new AmazonS3Client(AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, RegionEndpoint.USEast1))
                using (var s3Client = new AmazonS3Client(RegionEndpoint.USEast1))

                {
                    var transfer = new TransferUtility(s3Client);
                    await transfer.UploadDirectoryAsync(Properties.Settings.Default.OutputDirectory, BucketName, "*.*", SearchOption.AllDirectories);
                    Debug.WriteLine("Successfully uploaded directory to S3.");
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}' when writing an object");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown encountered on server. Message:'{e.Message}' when writing an object");
            }
        }



        /// <summary>
        /// Shows how to upload a file from the local computer to an Amazon S3
        /// bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The Amazon S3 bucket to which the object
        /// will be uploaded.</param>
        /// <param name="objectName">The object to upload.</param>
        /// <param name="filePath">The path, including file name, of the object
        /// on the local computer to upload.</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// upload procedure.</returns>
        public static async Task<bool> UploadFileAsync(
            IAmazonS3 client,
            string bucketName,
            string objectName,
            string filePath)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                    FilePath = filePath,
                };

                await client.PutObjectAsync(request);
                Debug.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                Debug.WriteLine($"Could not upload {objectName} to {bucketName}: '{ex.Message}'");
                return false;
            }
            catch (AmazonClientException ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not upload {objectName} to {bucketName}: '{ex.Message}'");
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private async Task UploadFlatFileAsync(string flatFile)
        {
            //using (var s3Client = new AmazonS3Client(AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, RegionEndpoint.USEast1))
            using (var s3Client = new AmazonS3Client(RegionEndpoint.USEast1))
            {
                var result = await UploadFileAsync(s3Client, BucketName, Path.GetFileName(flatFile), flatFile);
            }
        }


        private void InventoryViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsAllSelected))
            {
                foreach (var item in Items)
                {
                    item.IsSelected = IsAllSelected;
                }
            }

            if (e.PropertyName == nameof(SelectedItem))
            {
                Images.Clear();
                if (SelectedItem != null)
                {
                    if (SelectedItem.Images.Any())
                        Images.AddRange(SelectedItem.Images);
                }
            }
        }

        private bool _isAllSelected;
        private ProductItem _selectedItem;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ILiteDatabase _db;

        public bool IsAllSelected
        {
            get { return _isAllSelected; }
            set { Set(ref _isAllSelected, value); }
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            Task.Run(() => LoadItems());
        }

        private void LoadItems()
        {
            var items = _db.GetCollection<ProductItem>().FindAll();
            Items.Clear();
            OnUIThread(() => Items.AddRange(items));
        }

        public async void Delete()
        {
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Deleting Items", "Please wait...");

            try
            {
                var itemsCollection = _db.GetCollection<ProductItem>();
                var selected = Items.Where(i => i.IsSelected).ToList();


                for (int i = selected.Count - 1; i >= 0; i--)
                {
                    progress.SetMessage($"Deleting {selected[i].ItemName}...");
                    progress.SetProgress((double)(selected.Count - i - 1) / selected.Count);

                    for (int j = selected[i].Images.Count - 1; j >= 0; j--)
                    {
                        if (File.Exists(selected[i].Images[j].Path))
                        {
                            File.Delete(selected[i].Images[j].Path);
                        }
                    }

                    ProductItem item = selected[i];
                    itemsCollection.Delete(item.Id);
                    Items.Remove(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await progress.CloseAsync();
            }
        }
    }
}
