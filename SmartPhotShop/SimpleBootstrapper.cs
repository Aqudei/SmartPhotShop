using AutoMapper;
using Caliburn.Micro;
using dotenv.net;
using LiteDB;
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
            DotEnv.Load();

            Directory.CreateDirectory(Path.GetDirectoryName(Constants.DbPath));

            container = new SimpleContainer();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.Singleton<IWindowManager, WindowManager>();

            container.PerRequest<SplashViewModel>();
            container.PerRequest<MainViewModel>();
            container.PerRequest<SettingsViewModel>();
            container.PerRequest<RunViewModel>();
            container.PerRequest<ProductsViewModel>();
            container.PerRequest<InventoryViewModel>();
            container.PerRequest<FieldsViewModel>();
            container.PerRequest<FieldCrudViewModel>();
            container.Instance(DialogCoordinator.Instance);

            container.Instance<ILiteDatabase>(new LiteDatabase(Constants.DbPath));

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Properties.Settings, SettingsViewModel>().ReverseMap();
                cfg.CreateMap<ProductTemplate, ProductTemplate>();
                cfg.CreateMap<ProductTemplate, ProductItem>()
                    .ForMember(m => m.IsSelected, opts => opts.Ignore())
                    .ForMember(m => m.IsNotifying, opts => opts.Ignore())
                    .ForMember(m => m.Id, opts => opts.Ignore());
            });

            container.Instance(config.CreateMapper());


            var mapper = BsonMapper.Global;

            mapper.Entity<ProductItem>()
                .Ignore(x => x.IsNotifying)
                .Ignore(x => x.IsSelected);

            mapper.Entity<ProductTemplate>()
                .Ignore(x => x.IsNotifying);
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

            logger.Info("Application Started!");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            await DisplayRootViewForAsync<SplashViewModel>();
        }


        protected override void OnExit(object sender, EventArgs e)
        {
            using (var db = container.GetInstance<ILiteDatabase>())
            {
            }
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new List<Assembly> { Assembly.GetExecutingAssembly() };
    }
}
