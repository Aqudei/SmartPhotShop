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
                        var variants = variantsCollection.FindAll().ToList();

                        foreach (var variant in Variants)
                        {
                            if (!string.IsNullOrWhiteSpace(variant.VariantSku))
                            {
                                var v = variants.FirstOrDefault(vv => vv.VariantSku == variant.VariantSku);
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

            OnUIThread(() =>
            {
                Products.AddRange(products);
                SelectedProduct = Products.FirstOrDefault();
            });
        }

        

        internal void SaveItem(VariantTemplate item)
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var variantsCol = db.GetCollection<VariantTemplate>();
                if (item.Id == 0)
                {
                    variantsCol.Insert(item);
                }
                else
                {
                    variantsCol.Update(item.Id, item);
                }
            }
        }
    }
}
