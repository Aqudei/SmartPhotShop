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
        public string DbPath { get; }

        public BindableCollection<ProductImage> Images { get; set; } = new BindableCollection<ProductImage>();
        public BindableCollection<ProductItem> Items { get; set; } = new BindableCollection<ProductItem>();
        public ProductItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
        public InventoryViewModel(IDialogCoordinator dialogCoordinator)
        {
            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            DisplayName = "Files";

            PropertyChanged += InventoryViewModel_PropertyChanged;
            _dialogCoordinator = dialogCoordinator;

            PropertyChanged += InventoryViewModel_PropertyChanged1;
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
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Updating Flat File", "Please wait...");
            progress.SetIndeterminate();

            try
            {
                using (var excel = new ExcelPackage(Properties.Settings.Default.FlatFile))
                using (var db = new LiteDatabase(DbPath))
                {
                    var productTemplatesCollection = db.GetCollection<ProductTemplate>().FindAll();
                    var flatFile = Properties.Settings.Default.FlatFile;
                    var sheetName = "Template";
                    var productItems = db.GetCollection<ProductItem>().FindAll().ToList();


                    foreach (var productItem in productItems)
                    {
                        var data = new List<object> { productItem.Sku };

                        if (productItem != null)
                        {
                            data = new List<object> {
                                $"{productItem.Sku}",
                                $"{productItem.ProductId}",
                                $"{productItem.ProductIdType}",
                                productItem.Price,
                                productItem.MinimumSellerAllowedPrice,
                                productItem.MaximumSellerAllowedPrice,
                                $"{productItem.ItemCondition}",
                                productItem.Quantity,
                                $"{productItem.AddDelete}",
                                productItem.WillShipInternationally,
                                productItem.ExpeditedShipping,
                                $"{productItem.ItemNote}",
                                $"{productItem.FulfillmentCenterId}",
                                $"{productItem.MerchantShippingGroupName}",
                                $"{productItem.ProductTaxCode}",
                                productItem.HandlingTime,
                                productItem.BatteriesRequired ? "True" : "False",
                                productItem.AreBatteriesIncluded ? "True" : "False",
                                $"{productItem.BatteryCellComposition}",
                                $"{productItem.BatteryType}",
                                productItem.NumberOfBatteries,
                                productItem.BatteryWeight,
                                $"{productItem.BatteryWeightUnitOfMeasure}",
                                productItem.NumberOfLithiumIonCells,
                                productItem.NumberOfLithiumMetalCells,
                                $"{productItem.LithiumBatteryPackaging}",
                                productItem.LithiumBatteryEnergyContent,
                                $"{productItem.LithiumBatteryEnergyContentUnitOfMeasure}",
                                productItem.LithiumBatteryWeight,
                                $"{productItem.LithiumBatteryWeightUnitOfMeasure}",
                                $"{productItem.SupplierDeclaredDgHzRegulation1}",
                                $"{productItem.SupplierDeclaredDgHzRegulation2}",
                                $"{productItem.SupplierDeclaredDgHzRegulation3}",
                                $"{productItem.SupplierDeclaredDgHzRegulation4}",
                                $"{productItem.SupplierDeclaredDgHzRegulation5}",
                                $"{productItem.HazmatUnitedNationsRegulatoryId}",
                                $"{productItem.SafetyDataSheetUrl}",
                                productItem.ItemWeight,
                                $"{productItem.ItemWeightUnitOfMeasure}",
                                productItem.ItemVolume,
                                $"{productItem.ItemVolumeUnitOfMeasure}",
                                productItem.FlashPoint,
                                $"{productItem.GhsClassificationClass1}",
                                $"{productItem.GhsClassificationClass2}",
                                $"{productItem.GhsClassificationClass3}",
                                productItem.ListPriceWithTax,
                                productItem.UvpListPrice,
                        };

                            UpdateOrInsertRow(excel, flatFile, sheetName, (string)productItem.Sku, data);
                        }

                        Process.Start(Properties.Settings.Default.FlatFile);

                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
                await progress.CloseAsync();
            }
        }

        private void InventoryViewModel_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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

        private void InventoryViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsAllSelected))
            {
                foreach (var item in Items)
                {
                    item.IsSelected = IsAllSelected;
                }
            }
        }

        private bool _isAllSelected;
        private ProductItem _selectedItem;
        private readonly IDialogCoordinator _dialogCoordinator;

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
            using (var db = new LiteDatabase(DbPath))
            {
                var items = db.GetCollection<ProductItem>().FindAll();
                Items.Clear();
                OnUIThread(() => Items.AddRange(items));
            }
        }

        public async void Delete()
        {
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Deleting Items", "Please wait...");

            try
            {
                using (var db = new LiteDatabase(DbPath))
                {
                    var itemsCollection = db.GetCollection<ProductItem>();
                    var selected = Items.Where(i => i.IsSelected).ToList();


                    for (int i = selected.Count - 1; i >= 0; i--)
                    {
                        progress.SetMessage($"Deleting {selected[i].ProductName}...");
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
