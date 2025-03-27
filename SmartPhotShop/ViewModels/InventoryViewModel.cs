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
        public string DbPath { get; }

        public BindableCollection<OutputItem> Items { get; set; } = new BindableCollection<OutputItem>();
        public InventoryViewModel()
        {
            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            DisplayName = "Inventory";

            this.PropertyChanged += InventoryViewModel_PropertyChanged;
        }

        private void InventoryViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsAllSelected))
            {
                foreach (var item in Items)
                {
                    item.IsSelected = IsAllSelected;
                }
            }
        }

        private bool _isAllSelected;

        public bool IsAllSelected
        {
            get { return _isAllSelected; }
            set { Set(ref _isAllSelected, value); }
        }


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
