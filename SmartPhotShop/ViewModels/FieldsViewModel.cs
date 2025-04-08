using Caliburn.Micro;
using CsvHelper;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using OfficeOpenXml;
using SmartPhotShop.Events;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SmartPhotShop.ViewModels
{
    public class FieldsViewModel : Screen, IHandle<CrudEvent<Field>>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCoordinator;

        private BindableCollection<Field> _fields = new BindableCollection<Field>();
        public ICollectionView FieldsCollectionView { get; set; }
        public FieldsViewModel(IWindowManager windowManager, IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Fields";
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _dialogCoordinator = dialogCoordinator;
            _eventAggregator.SubscribeOnPublishedThread(this);

            FieldsCollectionView = CollectionViewSource.GetDefaultView(_fields);
        }
        protected override void OnViewAttached(object view, object context)
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                _fields.Clear();
                var collection = db.GetCollection<Field>();
                var fields = collection.FindAll();

                if (fields != null && fields.Any())
                {
                    _fields.AddRange(fields);
                }


                if (_fields.Any() && _fields.All(f => f.Order == 0))
                {
                    var order = 1;

                    foreach (var field in _fields)
                    {
                        field.Order = order++;
                        collection.Update(field);
                    }
                }
            }
        }

        public void NewField()
        {
            _windowManager.ShowDialogAsync(IoC.Get<FieldCrudViewModel>());
        }

        public void SaveChanges()
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                var collection = db.GetCollection<Field>();
                foreach (var item in _fields)
                {
                    collection.Update(item);
                }
            }
        }

        public async void ExportColumns()
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = "export", // Default file name (without extension)
                AddExtension = true,
                OverwritePrompt = true,
                Title = "Save CSV File"
            };

            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value)
            {
                return;
            }

            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Please wait", "Exporting columns...");
            try
            {
                using (var writer = new StreamWriter(dialog.FileName))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    // Write header for ImportExportItem
                    csv.WriteHeader<ImportExportItem>();
                    csv.NextRecord(); // Ensure the header line is completed

                    using (var db = new LiteDatabase(Constants.DbPath))
                    {
                        var fields = _fields.ToList();

                        for (int i = 0; i < fields.Count; i++)
                        {
                            var rec = fields[i];
                            csv.WriteRecord(rec);
                            csv.NextRecord(); // Move to the next line after writing a record
                            progress.SetProgress((i + 1) / (double)fields.Count);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
            }
            finally
            {
                await progress.CloseAsync();
            }
        }

        public async void ImportColumns()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select a CSV File",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".csv",
                Multiselect = false
            };

            //dialog.Filters.Add(new Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogFilter("CSV File", "*.csv"));
            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value)
            {
                return;
            }

            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Please wait", "Importing columns...");

            try
            {
                using (var reader = new StreamReader(dialog.FileName))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<ImportExportItem>().ToList();
                    using (var db = new LiteDatabase(Constants.DbPath))
                    {

                        var fields = db.GetCollection<Field>();
                        var headerCounts = new Dictionary<string, int>();

                        for (int i = 0; i < records.Count; i++)
                        {
                            var rec = records[i];
                            // Check if this header has already been used
                            if (headerCounts.ContainsKey(rec.Header))
                            {
                                headerCounts[rec.Header]++;
                            }
                            else
                            {
                                headerCounts[rec.Header] = 0;
                            }
                            
                            // Generate a new name based on occurrence
                            string uniqueName = headerCounts[rec.Header] == 0
                                ? rec.Header
                                : $"{rec.Header}.{headerCounts[rec.Header]}";

                            var newField = new Field
                            {
                                Group = rec.Group,
                                Name = uniqueName,
                                Type = rec.Type,
                            };

                            fields.Insert(newField);
                            await _eventAggregator.PublishOnUIThreadAsync(new Events.CrudEvent<Field>
                            {
                                CrudAction = CrudAction.Create,
                                Item = newField,
                            });

                            progress.SetProgress((i + 1) / (double)records.Count);
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                await progress.CloseAsync();
            }
        }
        public void DeleteField(Field field)
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                var fields = db.GetCollection<Field>();
                fields.Delete(field.Id);
                _fields.Remove(field);
            }
        }

        public void MoveUp(Field field)
        {
            var swapItem = _fields.OrderByDescending(f => f.Order).FirstOrDefault(f => f.Order < field.Order);
            if (swapItem != null)
            {
                var swapOrder = swapItem.Order;
                swapItem.Order = field.Order;
                field.Order = swapOrder;

                FieldsCollectionView.SortDescriptions.Clear();
                FieldsCollectionView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            }

          
        }

        public void MoveDown(Field field)
        {
            var swapItem = _fields.OrderBy(f => f.Order).FirstOrDefault(f => f.Order > field.Order);
            if (swapItem != null)
            {
                var swapOrder = swapItem.Order;
                swapItem.Order = field.Order;
                field.Order = swapOrder;

                FieldsCollectionView.SortDescriptions.Clear();
                FieldsCollectionView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            }
        }
        public async Task HandleAsync(CrudEvent<Field> message, CancellationToken cancellationToken)
        {
            await Task.Run(() => _fields.Add(message.Item));
        }
    }
}
