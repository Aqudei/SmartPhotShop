using AutoMapper;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    class SettingsViewModel : Screen
    {
        private string workingDirectory;
        private string errorDirectory;
        private string doneDirectory;
        private string outputDirectory;
        private readonly IMapper mapper;

        public string WorkingDirectory { get => workingDirectory; set => Set(ref workingDirectory, value); }
        public string ErrorDirectory { get => errorDirectory; set => Set(ref errorDirectory, value); }
        public string DoneDirectory { get => doneDirectory; set => Set(ref doneDirectory, value); }
        public string OutputDirectory { get => outputDirectory; set => Set(ref outputDirectory, value); }

        public SettingsViewModel(IMapper mapper)
        {
            DisplayName = "Settings";
            this.mapper = mapper;

            mapper.Map(Properties.Settings.Default, this);
        }

        public void Save()
        {
            mapper.Map(this, Properties.Settings.Default);
            Properties.Settings.Default.Save();

            Directory.CreateDirectory(WorkingDirectory);
            Directory.CreateDirectory(DoneDirectory);
            Directory.CreateDirectory(ErrorDirectory);
            Directory.CreateDirectory(OutputDirectory);
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
