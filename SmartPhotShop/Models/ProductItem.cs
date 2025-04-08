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
        private string _name;

        public string Name { get => _name; set => Set(ref _name , value); }
        public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
        public int Id { get; set; }

        // Supplier Description				
        public string SKU { get => _sku; set => Set(ref _sku, value); }
        public List<FieldValue> FieldValues { get; set; } = new List<FieldValue>();

        public BindableCollection<ProductImage> Images { get => _images; set => Set(ref _images, value); }

        public int ProductTemplateId { get; internal set; }
    }
}
