using Caliburn.Micro;
using LiteDB;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    internal class InventoryViewModel : Screen
    {
        public BindableCollection<OutputItem> Items { get; set; } = new BindableCollection<OutputItem>();
        public InventoryViewModel()
        {
            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            DisplayName = "Inventory";

        }

        public string DbPath { get; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            Task.Run(() => LoadItems());
        }

        private void LoadItems()
        {
            using (var db = new LiteDatabase(DbPath))
            {
                var items = db.GetCollection<OutputItem>().FindAll();
                Items.Clear();
                OnUIThread(() => Items.AddRange(items));
            }
        }

       
    }
}
