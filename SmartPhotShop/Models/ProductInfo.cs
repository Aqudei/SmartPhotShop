using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProductInfo
    {
        public string ProductDirectory { get; set; }
        public string ProductName { get; set; }

        public int DesignCount { get; set; }
        internal IEnumerable<DesignInfo> Designs { get; private set; }

        public ProductInfo(string productDirectory)
        {
            ProductDirectory = productDirectory;
            ProductName = Path.GetFileName(ProductDirectory);
            DesignCount = Directory.GetFiles(productDirectory, "*.*").Length;
            Designs = Directory.GetFiles(productDirectory, "*.*").Select(f => new DesignInfo(f)).ToArray();
        }

    }
}
