using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class Field : PropertyChangedBase
    {
        private int _order;

        public int Id { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public int Order { get => _order; set => Set(ref _order, value); }

    }

    public class FieldValue
    {

        public int FieldId { get; set; }
        public object Value { get; set; }
    }
}
