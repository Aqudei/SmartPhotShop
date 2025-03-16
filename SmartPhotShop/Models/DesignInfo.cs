using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    class DesignInfo
    {
        public string DesignPath { get; set; }
        public string DesignName { get; set; }

        public int DesignCount { get; set; }

        public DesignInfo(string designPath)
        {
            DesignPath = designPath;
            DesignName = Path.GetFileNameWithoutExtension(designPath);
        }
    }
}
