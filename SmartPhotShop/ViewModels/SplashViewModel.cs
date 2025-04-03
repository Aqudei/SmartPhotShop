using Caliburn.Micro;
using DocumentFormat.OpenXml.VariantTypes;
using LiteDB;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    internal class SplashViewModel : Screen
    {
        private readonly IWindowManager _windowManager;

        public string DbPath { get; }

        public SplashViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;

            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
        }

        private Task ImportProductsAsync()
        {
            return Task.Run(() =>
            {
                var products = Directory.GetDirectories(Properties.Settings.Default.ProductsDirectory)
                    .Select(d => new ProductTemplate(d));

                using (var db = new LiteDatabase(DbPath))
                {
                    var productTemplateCollection = db.GetCollection<ProductTemplate>();
                    foreach (var product in products)
                    {
                        var dbProduct = productTemplateCollection.FindOne(v => v.Sku == product.Sku);
                        if (dbProduct == null)
                            productTemplateCollection.Insert(product);
                        else
                            productTemplateCollection.Update(dbProduct.Id, product);
                    }
                }

            });
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            // Simulate loading delay
            await ImportProductsAsync();


            var mainViewModel = IoC.Get<MainViewModel>();
            await _windowManager.ShowWindowAsync(mainViewModel);

            // Close splash screen and open main window
            await TryCloseAsync();
        }
    }
}
