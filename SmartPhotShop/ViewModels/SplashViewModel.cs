using AutoMapper;
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
        private readonly IMapper _mapper;

        public SplashViewModel(IWindowManager windowManager, ILiteDatabase liteDatabase, IMapper mapper)
        {
            _windowManager = windowManager;
            _db = liteDatabase;
            _mapper = mapper;
        }

        private Task ImportNewProductsAsync()
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

                var skuComparer = new ProductTemplateComparer();

                var ignored = products.Intersect(productTemplateCollection.FindAll(), skuComparer);
                var newProducts = products.Except(ignored, skuComparer).ToList();
                var deletedProducts = productTemplateCollection.FindAll().Except(ignored, skuComparer).ToList();

                foreach (var newProduct in newProducts)
                {
                    productTemplateCollection.Insert(newProduct);
                }

                foreach (var deletedProduct in deletedProducts)
                {
                    productTemplateCollection.Delete(deletedProduct.Id);
                }
            });
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            // Simulate loading delay
            await ImportNewProductsAsync();

            var mainViewModel = IoC.Get<MainViewModel>();
            await _windowManager.ShowWindowAsync(mainViewModel);

            // Close splash screen and open main window
            await TryCloseAsync();
        }


    }
}
