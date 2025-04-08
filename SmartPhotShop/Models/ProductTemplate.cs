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
        public int Id { get; set; }
        public string ItemName { get; set; }
        public string SKU { get; set; }

        public List<FieldValue> FieldValues { get; set; } = new List<FieldValue>();

        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        public static ProductTemplate CreateFromPath(string path, ICollection<Field> fields)
        {
            var itemName = System.IO.Path.GetFileNameWithoutExtension(path);
            var productTemplate = new ProductTemplate
            {

                ItemName = itemName,
                SKU = Regex.Replace(itemName.ToUpper(), @"\s+", "-"),
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
                productTemplate.FieldValues.Add(new FieldValue
                {
                    FieldId = field.Id
                });
            }

            return productTemplate;
        }


        public ProductTemplate()
        { }
    }
}
