using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProcessItem : PropertyChangedBase
    {
        private string originalFileName;
        private string movedFileName;
        private string status;
        private DateTime? dateAdded;
        private ProductInfo product;

        public string Overlay { get => originalFileName; set => Set(ref originalFileName, value); }
        public string MovedFileName { get => movedFileName; set => Set(ref movedFileName, value); }
        public string Status { get => status; set => Set(ref status, value); }
        public DateTime? DateAdded { get => dateAdded; set => Set(ref dateAdded, value); }

        public ProductInfo Product { get => product; set => Set(ref product, value); }
        internal DesignInfo Design { get; set; }
        public string BaseImage => Design.DesignPath;
        public string Sku { get; set; }

        public ProcessItem(string overlay, DesignInfo baseDesign, ProductInfo product)
        {
            Design = baseDesign;
            Overlay = overlay;
            Product = product;
            DateAdded = DateTime.Now;
            Status = "Pending";

            var overlayName = Path.GetFileNameWithoutExtension(overlay);

            Sku = (product.ProductName + "-" + baseDesign.DesignName + "-" + overlayName).Replace(" ", "-").ToUpper();
        }
    }
}
