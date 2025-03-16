using Caliburn.Micro;
using DocumentFormat.OpenXml.Drawing.Diagrams;
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

        public BindableCollection<ProductInfo> Products { get; set; } = new BindableCollection<ProductInfo>();

        public ProductsViewModel()
        {
            DisplayName = "Products";
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
