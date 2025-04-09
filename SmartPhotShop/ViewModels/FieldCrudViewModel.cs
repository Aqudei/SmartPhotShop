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
            Types.Add(typeof(int));
            Types.Add(typeof(decimal));
            Types.Add(typeof(string));
            _eventAggregator = eventAggregator;
            _db = liteDatabase;
        }

        public async void Close()
        {
            await TryCloseAsync();
        }

        

        public async void Save()
        {
            var newField = new Field
            {
                Group = Group,
                Name = Name,
                Type = SelectedType.ToString(),
            };
            _db.GetCollection<Field>()
                .Insert(newField);

            await _eventAggregator.PublishOnUIThreadAsync(new Events.CrudEvent<Field>
            {
                CrudAction = Events.CrudAction.Create,
                Item = newField
            });
        }
    }
}
