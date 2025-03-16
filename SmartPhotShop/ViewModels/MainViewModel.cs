using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    class MainViewModel:Conductor<Screen>.Collection.OneActive
    {
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        public MainViewModel()
        {
            Items.Add(IoC.Get<RunViewModel>());
            Items.Add(IoC.Get<SettingsViewModel>());
            Items.Add(IoC.Get<ProductsViewModel>());

            ActiveItem = Items.First();

        }
    }
}
