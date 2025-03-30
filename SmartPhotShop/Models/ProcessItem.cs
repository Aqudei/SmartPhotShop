using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        internal VariantTemplate Variant { get; set; }
        public string BaseImage => Variant.VariantPath;
        public string Sku { get; set; }

        public ProcessItem(string overlay, VariantTemplate variant, ProductInfo product)
        {
            Variant = variant;
            Overlay = overlay;
            Product = product;
            DateAdded = DateTime.Now;
            Status = "Pending";

            var overlayName = Path.GetFileNameWithoutExtension(overlay);

            Sku = Regex.Replace($"{variant.VariantSku}-{overlayName}".ToUpper(), @"\s+", "-");
        }
    }
}
