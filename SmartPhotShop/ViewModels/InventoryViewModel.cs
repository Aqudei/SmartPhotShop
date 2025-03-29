using Caliburn.Micro;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public OutputItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }
        public InventoryViewModel(IDialogCoordinator dialogCoordinator)
        {
            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
            DisplayName = "Files";

            PropertyChanged += InventoryViewModel_PropertyChanged;
            _dialogCoordinator = dialogCoordinator;

            PropertyChanged += InventoryViewModel_PropertyChanged1;
        }

        private void InventoryViewModel_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedItem))
            {
                if (SelectedItem != null)
                {

                }
            }
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
        private OutputItem _selectedItem;
        private readonly IDialogCoordinator _dialogCoordinator;

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

        public async void Delete()
        {
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Deleting Items", "Please wait...");

            try
            {
                using (var db = new LiteDatabase(DbPath))
                {
                    var itemsCollection = db.GetCollection<OutputItem>();
                    var selected = Items.Where(i => i.IsSelected).ToList();


                    for (int i = selected.Count - 1; i >= 0; i--)
                    {
                        progress.SetMessage($"Deleting {selected[i].Location}...");
                        progress.SetProgress((double)(selected.Count - i - 1) / selected.Count);

                        File.Delete(selected[i].Location);
                        OutputItem item = selected[i];
                        itemsCollection.Delete(item.Id);
                        Items.Remove(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await progress.CloseAsync();
            }
        }
    }
}
