using Caliburn.Micro;
using LiteDB;
using OfficeOpenXml;
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

        public int Id { get => id; set => Set(ref id, value); }
        public string Group { get => group; set => Set(ref group, value); }
        public string Name { get => name; set => Set(ref name, value); }
        public Type SelectedType { get => _selectedType; set => Set(ref _selectedType, value); }


        public BindableCollection<Type> Types { get; set; } = new BindableCollection<Type>();

        public FieldCrudViewModel(IEventAggregator eventAggregator)
        {
            Types.Add(typeof(int));
            Types.Add(typeof(decimal));
            Types.Add(typeof(string));
            _eventAggregator = eventAggregator;
        }

        public async void Close()
        {
            await TryCloseAsync();
        }

        

        public async void Save()
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                var newField = new Field
                {
                    Group = Group,
                    Name = Name,
                    Type = SelectedType.ToString(),
                };
                db.GetCollection<Field>()
                    .Insert(newField);

                await _eventAggregator.PublishOnUIThreadAsync(new Events.CrudEvent<Field>
                {
                    CrudAction = Events.CrudAction.Create,
                    Item = newField
                });

            }
        }
    }
}
