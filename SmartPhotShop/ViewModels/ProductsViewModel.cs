using Caliburn.Micro;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using LiteDB;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace SmartPhotShop.ViewModels
{
    class ProductsViewModel : Screen
    {
        private ProductInfo selectedProduct;

        public BindableCollection<ProductInfo> Products { get; set; } = new BindableCollection<ProductInfo>();
        public BindableCollection<VariantTemplate> Variants { get; set; } = new BindableCollection<VariantTemplate>();
        public ProductInfo SelectedProduct { get => selectedProduct; set => Set(ref selectedProduct, value); }
        public string DbPath { get; }

        public ProductsViewModel()
        {
            DisplayName = "Products";

            PropertyChanged += ProductsViewModel_PropertyChanged;

            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
        }

        private void ProductsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedProduct))
            {
                Variants.Clear();

                if (SelectedProduct != null && SelectedProduct.Variants.Any())
                    Variants.AddRange(SelectedProduct.Variants);
            }
        }

        public void Save()
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var variants = db.GetCollection<VariantTemplate>();
                foreach (var item in Variants)
                {
                    var variant = variants.FindOne(v => v.Sku == item.Sku);
                    if (variant != null)
                    {
                        variant = item;
                        variants.Update(variant.Id, item);
                    }
                    else
                    {
                        variants.Insert(item);
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
