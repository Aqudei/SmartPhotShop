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
        private BindableCollection<FieldValue> _fields;
        private BindableCollection<ProductImage> _images;

        [BsonIgnore]
        public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
        public int Id { get; set; }

        // Supplier Description				
        public string SKU { get => _sku; set => Set(ref _sku, value); }
        public string ItemName { get; set; }

        public BindableCollection<ProductImage> Images { get => _images; set => Set(ref _images, value); }

        public BindableCollection<FieldValue> Fields { get => _fields; set => Set(ref _fields, value); }
        public int ProductTemplateId { get; internal set; }
    }
}
