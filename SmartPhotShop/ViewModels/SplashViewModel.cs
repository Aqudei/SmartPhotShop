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
        private readonly ILiteDatabase _db;

        public SplashViewModel(IWindowManager windowManager, ILiteDatabase liteDatabase)
        {
            _windowManager = windowManager;
            _db = liteDatabase;
        }

        private Task ImportProductsAsync()
        {
            return Task.Run(() =>
            {

                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ProductsDirectory) || !Directory.Exists(Properties.Settings.Default.ProductsDirectory))
                    return;

                var fieldsCollection = _db.GetCollection<Field>();
                var fields = fieldsCollection.FindAll().ToList();
                var products = Directory.GetDirectories(Properties.Settings.Default.ProductsDirectory)
                    .Select(d => ProductTemplate.CreateFromPath(d, fields));

                var productTemplateCollection = _db.GetCollection<ProductTemplate>();
                foreach (var product in products)
                {
                    var dbProduct = productTemplateCollection.FindOne(v => v.SKU == product.SKU);
                    if (dbProduct == null)
                        productTemplateCollection.Insert(product);
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
