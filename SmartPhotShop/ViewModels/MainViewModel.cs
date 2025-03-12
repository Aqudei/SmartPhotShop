using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    class MainViewModel:Conductor<Screen>.Collection.OneActive
    {
        public MainViewModel()
        {
            Items.Add(IoC.Get<RunViewModel>());
            Items.Add(IoC.Get<SettingsViewModel>());

            ActiveItem = Items.First();
        }
    }
}
