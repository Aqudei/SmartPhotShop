using AutoMapper;
using Caliburn.Micro;
using CsvHelper;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        public async void BrowseFlatFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Flat File",
                Filter = "CSV File (*.csv)|*.csv"

            };

            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            var prompt = await _dialogCoordinator.ShowMessageAsync(this, "Confirm", "Do you also want to read columns from this file?", MessageDialogStyle.AffirmativeAndNegative);
            FlatFile = dialog.FileName;

            if (prompt == MessageDialogResult.Affirmative)
            {
                ImportColumns(FlatFile);

                Properties.Settings.Default.FlatFile = FlatFile;
                Properties.Settings.Default.Save();
            }
        }

        private void ImportColumns(string flatFile)
        {
            using (var reader = new StreamReader(flatFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // Handle group logic if it contains "Supplier Description"
                List<string> groups = null;

                while (csv.Read())
                {
                    var row = ReadRowAsStrings(csv);
                    if (row == null || row.Count == 0) continue;


                    if (row[0] == "Supplier Description")
                    {
                        groups = NormalizeGroups(row);
                        continue;
                    }

                    // Handle header row
                    if (row[0] == "SKU")
                    {
                        if (groups == null || groups.Count != row.Count)
                        {
                            groups = Enumerable.Repeat("", row.Count).ToList(); // fallback to empty groups
                        }

                        var fieldCollection = _db.GetCollection<Field>();
                        fieldCollection.DeleteAll();
                        var productTemplatesCollection = _db.GetCollection<ProductTemplate>();
                        var productTemplates = productTemplatesCollection.FindAll().ToList();

                        foreach (var productTemplate in productTemplates)
                        {
                            productTemplate.FieldValues.Clear();
                            productTemplatesCollection.Update(productTemplate);
                        }

                        for (int i = 0; i < row.Count; i++)
                        {
                            var newField = new Field
                            {
                                Group = groups[i],
                                Name = row[i],
                                Type = typeof(string).ToString(),
                                Order = i + 1
                            };

                            fieldCollection.Insert(newField);
                            foreach (var productTemplate in productTemplates)
                            {
                                productTemplate.FieldValues.Add(new FieldValue
                                {
                                    FieldId = newField.Id,
                                    Value = ""
                                });

                                productTemplatesCollection.Update(productTemplate);
                            }

                        }
                    }
                }
            }
        }

        private List<string> ReadRowAsStrings(CsvReader csv)
        {
            var row = new List<string>();
            for (int i = 0; csv.TryGetField(i, out string field); i++)
            {
                row.Add(field);
            }
            return row;
        }

        private List<string> NormalizeGroups(List<string> row)
        {
            var groups = new List<string>();
            string lastGroup = "";

            foreach (var cell in row)
            {
                if (string.IsNullOrWhiteSpace(cell))
                {
                    groups.Add(lastGroup);
                }
                else
                {
                    lastGroup = cell;
                    groups.Add(cell);
                }
            }

            return groups;
        }


        public IEnumerable<IResult> Save()
        {
            yield return Task.Run(async () =>
            {
                _mapper.Map(this, Properties.Settings.Default);
                Properties.Settings.Default.Save();

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
