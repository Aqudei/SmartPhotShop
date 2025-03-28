using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    internal class OutputItem : PropertyChangedBase
    {
        private bool _isSelected;

        [LiteDB.BsonIgnore]
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public int Id { get; set; }

        public string Sku { get; set; }
        public int ProductId { get; set; }
        public string Location { get;  set; }
        public int VariantId { get;  set; }
    }
}
