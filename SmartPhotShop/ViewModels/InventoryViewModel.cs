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

        public BindableCollection<OutputItem> Items { get; set; } = new BindableCollection<OutputItem>();
        public OutputItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
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
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Updating Flat File", "Please wait...");
            progress.SetIndeterminate();

            try
            {
                using (var excel = new ExcelPackage(Properties.Settings.Default.FlatFile))
                using (var db = new LiteDatabase(DbPath))
                {
                    var variantsCollection = db.GetCollection<VariantTemplate>().FindAll();
                    var flatFile = Properties.Settings.Default.FlatFile;
                    var sheetName = "Template";
                    var concreteItems = db.GetCollection<OutputItem>().FindAll().ToList();


                    foreach (var item in concreteItems)
                    {
                        var data = new List<object> { item.Sku };

                        var variant = variantsCollection.FirstOrDefault(v => v.Id == item.VariantId);
                        if (variant != null)
                        {
                            data = new List<object> {
                                $"{item.Sku}",
                                $"{variant.ProductId}",
                                $"{variant.ProductIdType}",
                                variant.Price,
                                variant.MinimumSellerAllowedPrice,
                                variant.MaximumSellerAllowedPrice,
                                $"{variant.ItemCondition}",
                                variant.Quantity,
                                $"{variant.AddDelete}",
                                variant.WillShipInternationally,
                                variant.ExpeditedShipping,
                                $"{variant.ItemNote}",
                                $"{variant.FulfillmentCenterId}",
                                $"{variant.MerchantShippingGroupName}",
                                $"{variant.ProductTaxCode}",
                                variant.HandlingTime,
                                variant.BatteriesRequired ? "True" : "False",
                                variant.AreBatteriesIncluded ? "True" : "False",
                                $"{variant.BatteryCellComposition}",
                                $"{variant.BatteryType}",
                                variant.NumberOfBatteries,
                                variant.BatteryWeight,
                                $"{variant.BatteryWeightUnitOfMeasure}",
                                variant.NumberOfLithiumIonCells,
                                variant.NumberOfLithiumMetalCells,
                                $"{variant.LithiumBatteryPackaging}",
                                variant.LithiumBatteryEnergyContent,
                                $"{variant.LithiumBatteryEnergyContentUnitOfMeasure}",
                                variant.LithiumBatteryWeight,
                                $"{variant.LithiumBatteryWeightUnitOfMeasure}",
                                $"{variant.SupplierDeclaredDgHzRegulation1}",
                                $"{variant.SupplierDeclaredDgHzRegulation2}",
                                $"{variant.SupplierDeclaredDgHzRegulation3}",
                                $"{variant.SupplierDeclaredDgHzRegulation4}",
                                $"{variant.SupplierDeclaredDgHzRegulation5}",
                                $"{variant.HazmatUnitedNationsRegulatoryId}",
                                $"{variant.SafetyDataSheetUrl}",
                                variant.ItemWeight,
                                $"{variant.ItemWeightUnitOfMeasure}",
                                variant.ItemVolume,
                                $"{variant.ItemVolumeUnitOfMeasure}",
                                variant.FlashPoint,
                                $"{variant.GhsClassificationClass1}",
                                $"{variant.GhsClassificationClass2}",
                                $"{variant.GhsClassificationClass3}",
                                variant.ListPriceWithTax,
                                variant.UvpListPrice,
                        };

                            UpdateOrInsertRow(excel, flatFile, sheetName, item.Sku, data);
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
                if (SelectedItem != null)
                {

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
        private OutputItem _selectedItem;
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
                var items = db.GetCollection<OutputItem>().FindAll();
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
                    var itemsCollection = db.GetCollection<OutputItem>();
                    var selected = Items.Where(i => i.IsSelected).ToList();


                    for (int i = selected.Count - 1; i >= 0; i--)
                    {
                        progress.SetMessage($"Deleting {selected[i].Location}...");
                        progress.SetProgress((double)(selected.Count - i - 1) / selected.Count);

                        File.Delete(selected[i].Location);
                        OutputItem item = selected[i];
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
