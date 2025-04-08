using AutoMapper;
using Caliburn.Micro;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SmartPhotShop.ViewModels
{
    class SettingsViewModel : Screen
    {
        private string workingDirectory;
        private string errorDirectory;
        private string doneDirectory;
        private string outputDirectory;
        private string flatFile;
        private string productsDirectory;
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ILiteDatabase _db;
        private string _actionSet = "Test ATN";

        public string ActionSet
        {
            get { return _actionSet; }
            set { Set(ref _actionSet, value); }
        }

        public string WorkingDirectory { get => workingDirectory; set => Set(ref workingDirectory, value); }
        public string ProductsDirectory { get => productsDirectory; set => Set(ref productsDirectory, value); }
        public string ErrorDirectory { get => errorDirectory; set => Set(ref errorDirectory, value); }
        public string DoneDirectory { get => doneDirectory; set => Set(ref doneDirectory, value); }
        public string OutputDirectory { get => outputDirectory; set => Set(ref outputDirectory, value); }
        public string FlatFile { get => flatFile; set => Set(ref flatFile, value); }
        public string DbPath { get; }

        public SettingsViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator, ILiteDatabase db)
        {
            DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPhotoShop", "SmartPhotoShop.db");

            DisplayName = "Settings";
            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
            _db = db;
            mapper.Map(Properties.Settings.Default, this);
        }
        public static void RestartApp()
        {
            var exePath = Assembly.GetEntryAssembly().Location;

            Process.Start(exePath);
            Application.Current.Shutdown();
        }
        public async void ClearDatabase()
        {
            var prompt = await _dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to clear database?", MessageDialogStyle.AffirmativeAndNegative);
            if (prompt == MessageDialogResult.Affirmative)
            {
                _db.Dispose();

                if (File.Exists(DbPath))
                    File.Delete(DbPath);

                RestartApp();
            }
        }

        public void BrowseFlatFile()
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select Flat File"
            };
            dialog.Filters.Add(new CommonFileDialogFilter("Excel File", "*.xlsx"));

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            FlatFile = dialog.FileName;
        }
        public IEnumerable<IResult> Save()
        {
            yield return Task.Run(async () =>
            {
                OnUIThread(() =>
                {
                    _mapper.Map(this, Properties.Settings.Default);
                    Properties.Settings.Default.Save();
                });

                if (!string.IsNullOrWhiteSpace(WorkingDirectory) && !Directory.Exists(WorkingDirectory))
                    Directory.CreateDirectory(WorkingDirectory);

                if (!string.IsNullOrWhiteSpace(DoneDirectory) && !Directory.Exists(DoneDirectory))
                    Directory.CreateDirectory(DoneDirectory);

                if (!string.IsNullOrWhiteSpace(ErrorDirectory) && !Directory.Exists(ErrorDirectory))
                    Directory.CreateDirectory(ErrorDirectory);

                if (!string.IsNullOrWhiteSpace(OutputDirectory) && !Directory.Exists(OutputDirectory))
                    Directory.CreateDirectory(OutputDirectory);

                await _dialogCoordinator.ShowMessageAsync(this, "Success", "Your settings were successfully saved!");
            }).AsResult();
        }
        public void BrowseProductsDirectory()
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog { IsFolderPicker = true };

            if (dialog.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                return;


            ProductsDirectory = dialog.FileName;
        }
        public void BrowseWorkingDirectory()
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog { IsFolderPicker = true };

            if (dialog.ShowDialog() != Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                return;


            WorkingDirectory = dialog.FileName;

            ErrorDirectory = Path.Combine(WorkingDirectory, "Error");
            DoneDirectory = Path.Combine(WorkingDirectory, "Done");
            OutputDirectory = Path.Combine(WorkingDirectory, "Output");
        }
    }
}
