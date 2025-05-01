using Caliburn.Micro;
using LiteDB;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    public class FieldCrudViewModel : Screen
    {
        private int id;
        private string group;
        private string name;
        private Type _selectedType;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILiteDatabase _db;

        public int Id { get => id; set => Set(ref id, value); }
        public string Group { get => group; set => Set(ref group, value); }
        public string Name { get => name; set => Set(ref name, value); }
        public Type SelectedType { get => _selectedType; set => Set(ref _selectedType, value); }

        public BindableCollection<Type> Types { get; set; } = new BindableCollection<Type>();

        public FieldCrudViewModel(IEventAggregator eventAggregator, ILiteDatabase liteDatabase)
        {
            Types.Add(typeof(string));
            Types.Add(typeof(int));
            Types.Add(typeof(decimal));
            _eventAggregator = eventAggregator;
            _db = liteDatabase;

            SelectedType = Types.First();
        }

        public async void Close()
        {
            await TryCloseAsync();
        }

        public async void Save()
        {
            var fieldCollection = _db.GetCollection<Field>();
            var templateCollection = _db.GetCollection<ProductTemplate>();
            var itemCollection = _db.GetCollection<ProductItem>();
            var maxOrder = fieldCollection.Max(f => f.Order);
            var newField = new Field
            {
                Group = Group,
                Name = Name,
                Order = maxOrder + 1,
                Type = SelectedType.ToString(),
            };
            fieldCollection.Insert(newField);

            foreach (var template in templateCollection.FindAll())
            {
                template.FieldValues.Add(new FieldValue
                {
                    FieldId = newField.Id,
                });
            }

            foreach (var item in itemCollection.FindAll())
            {
                item.FieldValues.Add(new FieldValue
                {
                    FieldId = newField.Id,
                });
            }

            await _eventAggregator.PublishOnUIThreadAsync(new Events.CrudEvent<Field>
            {
                CrudAction = Events.CrudAction.Create,
                Item = newField
            });
        }
    }
}
