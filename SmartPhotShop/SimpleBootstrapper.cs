using AutoMapper;
using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using OfficeOpenXml;
using SmartPhotShop.Models;
using SmartPhotShop.ViewModels;
using SmartPhotShop.Views;
using System;
using System.Collections.Generic;
using System.IO;
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

            container.PerRequest<SplashViewModel>();
            container.PerRequest<MainViewModel>();
            container.PerRequest<SettingsViewModel>();
            container.PerRequest<RunViewModel>();
            container.PerRequest<ProductsViewModel>();
            container.PerRequest<InventoryViewModel>();
            container.Instance(DialogCoordinator.Instance);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Properties.Settings, SettingsViewModel>().ReverseMap();
                cfg.CreateMap<ProductTemplate, ProductTemplate>()
                .ForMember(v => v.ProductName, opts => opts.Ignore())
                .ForMember(v => v.Path, opts => opts.Ignore());
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
            //var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");
            //if (File.Exists(dbPath))
            //{
            //    var result = MessageBox.Show("Database already exists. Do you want to delete the existing database?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            //    if (result == MessageBoxResult.OK)
            //    {
            //        File.Delete(dbPath);
            //    }
            //}

            logger.Info("Application Started!");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            await DisplayRootViewForAsync<SplashViewModel>();
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new List<Assembly> { Assembly.GetExecutingAssembly() };
    }
}
