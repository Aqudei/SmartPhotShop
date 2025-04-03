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
        private ProductTemplate _selectedProductTemplate;
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;

        public BindableCollection<ProductTemplate> ProductTemplates { get; set; } = new BindableCollection<ProductTemplate>();
        public ProductTemplate SelectedProductTemplate { get => _selectedProductTemplate; set => Set(ref _selectedProductTemplate, value); }
        public string DbPath { get; }

        public ProductsViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Products";


            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
        }

        public void Save()
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var variants = db.GetCollection<ProductTemplate>();
                foreach (var item in ProductTemplates)
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
            ProductTemplates.Clear();
            using (var db = new LiteDatabase(DbPath))
            {
                var products = db.GetCollection<ProductTemplate>().FindAll().ToList();

                if (products == null || !products.Any())
                    return;

                OnUIThread(() =>
                {
                    ProductTemplates.AddRange(products);
                    SelectedProductTemplate = ProductTemplates.FirstOrDefault();
                });
            }
        }



        internal void SaveItem(ProductTemplate item)
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var variantsCol = db.GetCollection<ProductTemplate>();
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
