using AutoMapper;
using Caliburn.Micro;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using LiteDB;
using MahApps.Metro.Controls.Dialogs;
using OfficeOpenXml;
using SmartPhotShop.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Data;

namespace SmartPhotShop.ViewModels
{
    class ProductsViewModel : Screen
    {
        private ProductTemplate _selectedProductTemplate;

        private BindableCollection<ProductTemplate> _productTemplates = new BindableCollection<ProductTemplate>();
        private BindableCollection<FieldValueViewModel> _fieldValues = new BindableCollection<FieldValueViewModel>();

        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly ILiteDatabase _db;

        public ICollectionView ProductTemplates { get; set; }
        public ICollectionView FieldValues { get; set; }

        public ProductTemplate SelectedProductTemplate { get => _selectedProductTemplate; set => Set(ref _selectedProductTemplate, value); }

        public ProductsViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator, ILiteDatabase liteDatabase)
        {
            DisplayName = "Products";

            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
            _db = liteDatabase;
            ProductTemplates = CollectionViewSource.GetDefaultView(_productTemplates);
            FieldValues = CollectionViewSource.GetDefaultView(_fieldValues);

            PropertyChanged += ProductsViewModel_PropertyChanged;
        }

        private void ProductsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (nameof(SelectedProductTemplate).Equals(e.PropertyName) && SelectedProductTemplate != null)
            {
                _fieldValues.Clear();
                var collection = _db.GetCollection<Field>();

                foreach (var item in SelectedProductTemplate.FieldValues)
                {
                    var field = collection.FindOne(f => f.Id == item.FieldId);
                    var fieldValue = new FieldValueViewModel
                    {
                        FieldId = item.FieldId,
                        Group = field.Group,
                        Id = item.FieldId,
                        Name = field.Name,
                        Order = field.Order,
                        Type = field.Type,
                        Value = item.Value
                    };

                    _fieldValues.Add(fieldValue);
                }
            }
        }

        public void SaveChanges()
        {
            if (_fieldValues == null || !_fieldValues.Any())
                return;

            if (SelectedProductTemplate == null)
                return;

            var collection = _db.GetCollection<ProductTemplate>();
            var existingProductTemplate = collection.FindById(SelectedProductTemplate.Id);

            foreach (var fieldValue in _fieldValues)
            {
                var uiFieldValue = existingProductTemplate.FieldValues.FirstOrDefault(f => f.FieldId == fieldValue.FieldId);
                uiFieldValue.Value = fieldValue.Value;
                collection.Update(existingProductTemplate);
            }
        }

        protected override void OnViewAttached(object view, object context)
        {
            var products = _db.GetCollection<ProductTemplate>().FindAll().ToList();

            if (products == null || !products.Any())
                return;

            OnUIThread(() =>
            {
                _productTemplates.Clear();
                _productTemplates.AddRange(products);
            });

        }

        //public void SaveItem(DataRowView item, object header)
        //{
        //    using (var db = new LiteDatabase(Constants.DbPath))
        //    {
        //        var sku = item.Row.Field<string>("SKU");

        //        var collection = db.GetCollection<ProductTemplate>();
        //        var dbProject = collection.FindOne(p => p.SKU == sku);

        //        if (dbProject != null)
        //        {
        //            var incomingValue = item.Row.Field<object>(header.ToString());
        //            var field = db.GetCollection<Field>().FindOne(f => f.Name == header.ToString());

        //            var fieldValue = dbProject.FieldValues.FirstOrDefault(f => f.FieldId == field.Id);
        //            fieldValue.Value = incomingValue;
        //            collection.Update(dbProject.Id, dbProject);
        //        }
        //    }
        //}
    }
}
