using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Caliburn.Micro;
using ControlzEx.Standard;
using CsvHelper;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using OfficeOpenXml;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SmartPhotShop.ViewModels
{
    public class InventoryViewModel : Screen
    {
        //private string AWS_ACCESS_KEY_ID = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID", EnvironmentVariableTarget.User) ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID", EnvironmentVariableTarget.Machine);
        //private string AWS_SECRET_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

        public BindableCollection<ProductImage> Images { get; set; } = new BindableCollection<ProductImage>();
        public BindableCollection<ProductItem> Items { get; set; } = new BindableCollection<ProductItem>();
        public ProductItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
        public BindableCollection<FieldValueViewModel> _fieldValues = new BindableCollection<FieldValueViewModel>();
        public ICollectionView FieldValues { get; set; }
        public InventoryViewModel(IDialogCoordinator dialogCoordinator, ILiteDatabase liteDatabase)
        {
            DisplayName = "Files";

            PropertyChanged += InventoryViewModel_PropertyChanged;
            _dialogCoordinator = dialogCoordinator;
            _db = liteDatabase;

            FieldValues = CollectionViewSource.GetDefaultView(_fieldValues);

        }

        static void AppendCsvRow(CsvWriter csvWriter, string sheetName, string sku, List<object> newData)
        {

        }


        public async void UpdateFlatFile()
        {
            await Task.Run(UpdateFlatFileTask);
        }


        private List<string> ReadRowAsObjects(CsvReader csv)
        {
            var row = new List<string>();
            for (int i = 0; csv.TryGetField(i, out string field); i++)
            {
                row.Add(field);
            }
            return row;
        }
        public async Task UpdateFlatFileTask()
        {
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Please wait", "Updating Flat File...");
            progress.SetIndeterminate();

            try
            {
                var productTemplatesCollection = _db.GetCollection<ProductTemplate>();
                var flatFileTemplate = Properties.Settings.Default.FlatFile;
                var sheetName = "Template";
                var productItems = _db.GetCollection<ProductItem>().FindAll().ToList();

                var flatFileOutput = Path.Combine(Path.GetDirectoryName(flatFileTemplate), "InventoryFlatFile.csv");

                var skuField = _db.GetCollection<Field>().FindOne(f => f.Name == "SKU");

                using (var reader = new StreamReader(flatFileTemplate))
                using (var writer = new StreamWriter(flatFileOutput))
                using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    while (csvReader.Read())
                    {
                        var row = ReadRowAsObjects(csvReader);
                        if (row == null || row.Count == 0) continue;
                        foreach (var cell in row)
                        {
                            csvWriter.WriteField(cell);
                        }
                        csvWriter.NextRecord();
                    }

                    foreach (var productItem in productItems)
                    {
                        var rowData = new List<object> { productItem.SKU };
                        var productTemplate = productTemplatesCollection.FindById(productItem.ProductTemplateId);

                        var fieldValues = productItem.FieldValues.Where(f => f.FieldId != skuField.Id).Select(f => new
                        {
                            f.FieldId,
                            f.Value,
                            _db.GetCollection<Field>().FindById(f.FieldId).Order
                        });

                        foreach (var fieldValue in fieldValues.OrderBy(f => f.Order))
                        {
                            rowData.Add(fieldValue.Value);
                        }

                        foreach (var cell in rowData)
                        {
                            csvWriter.WriteField(cell);
                        }

                        csvWriter.NextRecord();
                    }
                }

                // await UploadFlatFileAsync(Properties.Settings.Default.FlatFile);

                progress.SetMessage("Uploading output files to S3...");
                await SyncProductItems();
                await progress.CloseAsync();
            }
            catch (Exception ex)
            {
                await progress.CloseAsync();
                await _dialogCoordinator.ShowMessageAsync(this, "Error", ex.Message);
            }
            finally
            {

            }
        }

        private async Task SyncProductItems()
        {
            try
            {
                //using (var s3Client = new AmazonS3Client(AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, RegionEndpoint.USEast1))
                using (var s3Client = new AmazonS3Client(RegionEndpoint.EUNorth1))

                {
                    var transfer = new TransferUtility(s3Client);
                    await transfer.UploadDirectoryAsync(Properties.Settings.Default.OutputDirectory, Constants.BucketName, "*.*", SearchOption.AllDirectories);
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
                var result = await UploadFileAsync(s3Client, Constants.BucketName, Path.GetFileName(flatFile), flatFile);
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

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);

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
                    progress.SetMessage($"Deleting {selected[i].SKU}...");
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
