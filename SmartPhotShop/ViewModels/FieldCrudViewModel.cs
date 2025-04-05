using Caliburn.Micro;
using System;
using System.Collections.Generic;
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

        public int Id { get => id; set => Set(ref id, value); }
        public string Group { get => group; set => Set(ref group, value); }
        public string Name { get => name; set => Set(ref name, value); }
        public Type SelectedType { get => _selectedType; set => Set(ref _selectedType, value); }


        public BindableCollection<Type> Types { get; set; } = new BindableCollection<Type>();

        public FieldCrudViewModel()
        {
            Types.Add(typeof(int));
            Types.Add(typeof(decimal));
            Types.Add(typeof(string));
        }
    }
}
