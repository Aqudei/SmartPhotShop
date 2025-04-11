using Caliburn.Micro;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.VariantTypes;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProductTemplate : PropertyChangedBase
    {
        private string _name;

        public int Id { get; set; }
        public string SKU { get; set; }

        public List<FieldValue> FieldValues { get; set; } = new List<FieldValue>();
        public string Name { get => _name; set => Set(ref _name, value); }

        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        public static ProductTemplate CreateFromPath(string path, ICollection<Field> fields)
        {
            var itemName = System.IO.Path.GetFileNameWithoutExtension(path);
            var productTemplate = new ProductTemplate
            {
                SKU = Regex.Replace(Regex.Replace(itemName.ToUpper(), @"\s+", "-"), @"-+", "-"),
                Name = itemName,
            };

            foreach (var image in Directory.GetFiles(path, "*.*"))
            {
                productTemplate.Images.Add(new ProductImage
                {
                    Path = image,
                    Name = System.IO.Path.GetFileNameWithoutExtension(image)
                });
            }

            foreach (var field in fields)
            {

                var newField = new FieldValue
                {
                    FieldId = field.Id
                };

                if (field.Name == "SKU")
                    newField.Value = productTemplate.SKU;

                if (field.Name == "Item Name")
                    newField.Value = productTemplate.Name;

                var fvExisting = productTemplate.FieldValues.FirstOrDefault(fv => fv.FieldId == field.Id);

                if (fvExisting != null)
                {
                    fvExisting.Value = newField.Value;
                    fvExisting.FieldId = newField.FieldId;
                }
                else
                    productTemplate.FieldValues.Add(newField);
            }

            return productTemplate;
        }


        public ProductTemplate()
        { }
    }
}
