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

        public override string ToString()
        {
            return $"{Name}::{Group}";
        }

    }

    public class FieldValue : IEquatable<FieldValue>
    {
        public int FieldId { get; set; }
        public object Value { get; set; }

        public bool Equals(FieldValue other)
        {
            if (other == null) return false;

            return FieldId == other.FieldId &&
                   Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FieldValue);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + FieldId.GetHashCode();
            hash = hash * 23 + (Value != null ? Value.GetHashCode() : 0);
            return hash;
        }

        public override string ToString()
        {
            return $"{FieldId}::{Value}";
        }
    }
}
