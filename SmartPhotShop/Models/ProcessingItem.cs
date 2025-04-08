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
    public class ProcessingItem : PropertyChangedBase
    {
        private string originalFileName;
        private string movedFileName;
        private string status;
        private DateTime? dateAdded;
        private ProductTemplate product;

        public string Overlay { get => originalFileName; set => Set(ref originalFileName, value); }
        public string MovedFileName { get => movedFileName; set => Set(ref movedFileName, value); }
        public string Status { get => status; set => Set(ref status, value); }
        public DateTime? DateAdded { get => dateAdded; set => Set(ref dateAdded, value); }
        internal ProductTemplate ProductTemplate { get; set; }
        public string Sku { get; set; }

        public ProcessingItem(string overlay, ProductTemplate productTemplate)
        {
            Overlay = overlay;
            ProductTemplate = productTemplate;
            DateAdded = DateTime.Now;
            Status = "Pending";

            var overlayName = Path.GetFileNameWithoutExtension(overlay);

            Sku = Regex.Replace($"{productTemplate.SKU}-{overlayName}".ToUpper(), @"\s+", "-");
        }
    }
}
