using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Events
{
    public enum CrudAction
    {
        Create = 0,
        Update,
        Delete,
    }
    public class CrudEvent<T>
    {
        public CrudAction CrudAction { get; set; }
        public T Item { get; set; }
    }
}
