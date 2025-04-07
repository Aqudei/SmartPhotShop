using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    internal class ImportExportItem
    {
        [Name("header")]
        public string Header { get; set; }
        [Name("group")]
        public string Group { get; set; }
        [Name("type")]
        public string Type { get; set; } = "System.String";
    }
}
