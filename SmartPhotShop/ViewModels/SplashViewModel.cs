using Caliburn.Micro;
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
                    .Select(d => new ProductInfo(d));

                using (var db = new LiteDatabase(DbPath))
                {
                    var variantsCol = db.GetCollection<VariantTemplate>();
                    foreach (var product in products)
                    {
                        foreach (var variant in product.Variants)
                        {
                            var dbVariant = variantsCol.FindOne(v => v.VariantSku == variant.VariantSku);
                            if (dbVariant == null)
                            {
                                variantsCol.Insert(variant);
                            }
                        }
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
