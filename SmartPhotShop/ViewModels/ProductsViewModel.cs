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
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;

namespace SmartPhotShop.ViewModels
{
    class ProductsViewModel : Screen
    {
        private ProductTemplate _selectedProductTemplate;
        private System.Data.DataView _productTemplates;
        private readonly IMapper _mapper;
        private readonly IDialogCoordinator _dialogCoordinator;

        public System.Data.DataView ProductTemplates { get => _productTemplates; set => Set(ref _productTemplates, value); }
        public ProductTemplate SelectedProductTemplate { get => _selectedProductTemplate; set => Set(ref _selectedProductTemplate, value); }

        public ProductsViewModel(IMapper mapper, IDialogCoordinator dialogCoordinator)
        {
            DisplayName = "Products";

            _mapper = mapper;
            _dialogCoordinator = dialogCoordinator;
        }

        public void Save()
        {
            //using (var db = new LiteDatabase(Constants.DbPath))
            //{
            //    var variants = db.GetCollection<ProductTemplate>();
            //    foreach (var item in ProductTemplates)
            //    {
            //        if (item.Id > 0)
            //        {
            //            variants.Update(item.Id, item);
            //        }
            //        else
            //        {
            //            var id = variants.Insert(item);
            //            item.Id = id;
            //        }
            //    }
            //}
        }

        protected override void OnViewAttached(object view, object context)
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                var products = db.GetCollection<ProductTemplate>().FindAll().ToList();

                if (products == null || !products.Any())
                    return;

                var dt = new System.Data.DataTable();

                for (int i = 0; i < products.Count; i++)
                {
                    var values = new List<object>();

                    ProductTemplate productTemplate = products[i];

                    if (i == 0)
                    {
                        dt.Columns.Add("SKU", typeof(string));
                        dt.Columns.Add("Item Name", typeof(string));

                        foreach (var fieldValue in productTemplate.Fields)
                        {
                            var field = db.GetCollection<Field>().FindOne(f => f.Id == fieldValue.FieldId);
                            if (field == null)
                            {
                                dt.Columns.Add("Unknown", typeof(string));
                                continue;
                            }

                            Type colType;
                            switch (field.Type)
                            {
                                case "System.Int32":
                                    colType = typeof(int);
                                    break;
                                case "System.Decimal":
                                    colType = typeof(decimal);
                                    break;
                                case "System.Boolean":
                                    colType = typeof(bool);
                                    break;
                                case "System.DateTime":
                                    colType = typeof(DateTime);
                                    break;
                                default:
                                    colType = typeof(string);
                                    break;
                            }

                            dt.Columns.Add(field.Name, colType);
                        }
                    }

                    values.Add(productTemplate.SKU);
                    values.Add(productTemplate.ItemName);

                    foreach (var fieldValue in productTemplate.Fields)
                    {
                        values.Add(fieldValue.Value ?? DBNull.Value);
                    }

                    dt.Rows.Add(values.ToArray());
                }

                OnUIThread(() =>
                {
                    ProductTemplates = dt.DefaultView;
                });
            }

        }

        public void SaveItem(DataRowView item, object header)
        {
            using (var db = new LiteDatabase(Constants.DbPath))
            {
                var sku = item.Row.Field<string>("SKU");

                var collection = db.GetCollection<ProductTemplate>();
                var dbProject = collection.FindOne(p => p.SKU == sku);

                if (dbProject != null)
                {
                    var incomingValue = item.Row.Field<object>(header.ToString());
                    var field = db.GetCollection<Field>().FindOne(f=>f.Name==header.ToString());

                    var fieldValue = dbProject.Fields.FirstOrDefault(f => f.FieldId == field.Id);
                    fieldValue.Value = incomingValue;
                    collection.Update(dbProject.Id, dbProject);
                }
            }
        }
    }
}
