using Caliburn.Micro;
using LiteDB;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.ViewModels
{
    public class FieldsViewModel : Screen
    {
        private readonly IWindowManager _windowManager;

        public BindableCollection<Field> Fields { get; set; } = new BindableCollection<Field>();
        public FieldsViewModel(IWindowManager windowManager)
        {
            DisplayName = "Fields";
            _windowManager = windowManager;
        }
        protected override void OnViewAttached(object view, object context)
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                Fields.Clear();

                var fields = db.GetCollection<Field>().FindAll();
                if (fields != null && fields.Any())
                {
                    Fields.AddRange(fields);
                }
            }
        }

        public void NewField()
        {
            _windowManager.ShowDialogAsync(IoC.Get<FieldCrudViewModel>());
        }
    }
}
