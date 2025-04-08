using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    public class FieldValueViewModel:PropertyChangedBase
    {
        private int _order;
        private int _fieldId;
        private object _value;
        private int _id;
        private string _group;
        private string _name;
        private string _type;

        public int FieldId { get => _fieldId; set => Set(ref _fieldId, value); }
        public object Value { get => _value; set => Set(ref _value, value); }
        public int Id { get => _id; set => Set(ref _id, value); }
        public string Group { get => _group; set => Set(ref _group, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public string Type { get => _type; set => Set(ref _type, value); }
        public int Order { get => _order; set => Set(ref _order, value); }
    }
}
