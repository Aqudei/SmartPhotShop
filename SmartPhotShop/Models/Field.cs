using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class Field
    {
        public int Id { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }

    }

    public class FieldValue
    {
        public int FieldId { get; set; }
        public object Value { get; set; }
    }
}
