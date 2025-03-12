using AutoMapper;
using Caliburn.Micro;
using NLog;
using SmartPhotShop.ViewModels;
using SmartPhotShop.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LogManager = NLog.LogManager;

namespace SmartPhotShop
{
    class SimpleBootstrapper : BootstrapperBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private SimpleContainer container;
        public SimpleBootstrapper()
        {
            Initialize();
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void Configure()
        {
            container = new SimpleContainer();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.Singleton<IWindowManager, WindowManager>();

            container.PerRequest<MainViewModel>();
            container.PerRequest<SettingsViewModel>();
            container.PerRequest<RunViewModel>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Properties.Settings, SettingsViewModel>().ReverseMap();
            });

            container.Instance(config.CreateMapper());
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            await DisplayRootViewForAsync<MainViewModel>();

            logger.Info("Application Started!");
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new List<Assembly> { Assembly.GetExecutingAssembly() };
    }
}
