using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProcessItem : PropertyChangedBase
    {
        private string originalFileName;
        private string movedFileName;
        private string status;
        private DateTime? dateAdded;

        public string OriginalFileName { get => originalFileName; set => Set(ref originalFileName , value); }
        public string MovedFileName { get => movedFileName; set => Set(ref movedFileName , value); }
        public string Status { get => status; set => Set(ref status, value); }
        public DateTime? DateAdded { get => dateAdded; set => Set(ref dateAdded, value); }

    }
}
