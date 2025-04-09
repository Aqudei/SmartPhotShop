using Caliburn.Micro;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProductItem : PropertyChangedBase
    {
        private bool _isSelected;
        private string _sku;
        private BindableCollection<ProductImage> _images;
        private BindableCollection<FieldValue> _fieldValues;
        private string _name;

        public string Name { get => _name; set => Set(ref _name, value); }
        public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
        public int Id { get; set; }

        // Supplier Description				
        public string SKU { get => _sku; set => Set(ref _sku, value); }

        public BindableCollection<ProductImage> Images { get => _images; set => Set(ref _images, value); }
        public BindableCollection<FieldValue> FieldValues { get => _fieldValues; set => Set(ref _fieldValues, value); }

        public int ProductTemplateId { get; internal set; }

        public void SetFieldValue(IQueryable<Field> fields, string fieldValueName, string value)
        {
            if (FieldValues == null || !FieldValues.Any())
                return;
            var field = fields.FirstOrDefault(f => f.Name == fieldValueName);
            if (field == null)
                return;

            var fieldValue = FieldValues.FirstOrDefault(f => f.FieldId == field.Id);

            if (fieldValue == null)
            {
                fieldValue = new FieldValue
                {
                    FieldId = field.Id,
                    Value = value
                };
                FieldValues.Add(fieldValue);
            }
            else
            {
                fieldValue.Value = value;
            }

        }
    }
}
