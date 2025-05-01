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


        public IEnumerable<string> GetFieldValues(IQueryable<Field> fieldsRef, string fieldValueName)
        {
            if (FieldValues == null || !FieldValues.Any())
                yield break;

            var targetFields = fieldsRef.Where(f => f.Name == fieldValueName).ToArray();

            for (int i = 0; i < targetFields.Length; i++)
            {
                var targetField = targetFields[i];
                var targetFieldValue = FieldValues.FirstOrDefault(f => f.FieldId == targetField.Id);
                if (targetFieldValue == null)
                    continue;

                if (i >= targetFields.Length)
                    break;

                yield return targetFieldValue.Value?.ToString();
            }
        }

        public void SetFieldValues(IQueryable<Field> fieldsRef, string fieldValueName, params string[] values)
        {
            if (FieldValues == null || !FieldValues.Any())
                return;
            var targetFields = fieldsRef.Where(f => f.Name == fieldValueName).ToArray();
            if (targetFields == null || !targetFields.Any())
                return;

            for (int i = 0; i < targetFields.Length; i++)
            {
                var targetField = targetFields[i];

                var targetFieldValue = FieldValues.FirstOrDefault(f => f.FieldId == targetField.Id);

                if (targetFieldValue == null)
                    continue;

                if (i >= values.Length)
                    break;

                targetFieldValue.Value = values?[i];
            }
        }
    }
}
