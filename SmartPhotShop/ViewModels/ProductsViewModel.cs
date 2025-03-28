using AutoMapper;
using Caliburn.Micro;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using OfficeOpenXml;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace SmartPhotShop.ViewModels
{
    class ProductsViewModel : Screen
    {
        private ProductInfo selectedProduct;
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;

        public BindableCollection<ProductInfo> Products { get; set; } = new BindableCollection<ProductInfo>();
        public BindableCollection<VariantTemplate> Variants { get; set; } = new BindableCollection<VariantTemplate>();
        public ProductInfo SelectedProduct { get => selectedProduct; set => Set(ref selectedProduct, value); }
        public string DbPath { get; }

        public ProductsViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Products";

            PropertyChanged += ProductsViewModel_PropertyChanged;

            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
        }

        private void ProductsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedProduct))
            {
                Variants.Clear();

                if (SelectedProduct != null && SelectedProduct.Variants.Any())
                {
                    Variants.AddRange(SelectedProduct.Variants);

                    using (var db = new LiteDatabase(DbPath))
                    {
                        var variantsCollection = db.GetCollection<VariantTemplate>();
                        var variants = variantsCollection.FindAll();

                        foreach (var variant in Variants)
                        {
                            if (!string.IsNullOrWhiteSpace(variant.Sku))
                            {
                                var v = variants.FirstOrDefault(vv => vv.Sku == variant.Sku);
                                if (v != null)
                                {
                                    _mapper.Map(v, variant);
                                }
                            }
                        }
                    }
                }
            }
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
                    var concreteItems = db.GetCollection<OutputItem>().FindAll();


                    foreach (var item in concreteItems)
                    {
                        var data = new[] { item.Sku };

                        var variant = variantsCollection.FirstOrDefault(v => v.Id == item.VariantId);
                        if (variant != null)
                        {
                            data = new[] {
                            item.Sku,
                            variant.ProductId,
                            variant.ProductIdType,
                            variant.Price.ToString()
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


        public void Save()
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var variants = db.GetCollection<VariantTemplate>();
                foreach (var item in Variants)
                {
                    if (item.Id > 0)
                    {
                        variants.Update(item.Id, item);
                    }
                    else
                    {
                        var id = variants.Insert(item);
                        item.Id = id;
                    }
                }
            }
        }

        protected override void OnViewLoaded(object view)
        {
            Products.Clear();

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ProductsDirectory))
            {
                return;
            }

            if (!Directory.Exists(Properties.Settings.Default.ProductsDirectory))
            {
                Directory.CreateDirectory(Properties.Settings.Default.ProductsDirectory);
            }
            var products = Directory.GetDirectories(Properties.Settings.Default.ProductsDirectory)
                .Select(d => new ProductInfo(d));
            Products.AddRange(products);
        }
    }
}
