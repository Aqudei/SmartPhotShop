using Caliburn.Micro;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.VariantTypes;
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
        public string ProductName { get; set; }
        public string Path { get; set; }
        public string Sku { get; set; }
        public string ProductId { get; set; }
        public string ProductIdType { get; set; }
        public decimal Price { get; set; }
        public decimal MinimumSellerAllowedPrice { get; set; }
        public decimal MaximumSellerAllowedPrice { get; set; }
        public string ItemCondition { get; set; }
        public int Quantity { get; set; }
        public string AddDelete { get; set; }
        public int WillShipInternationally { get; set; }
        public int ExpeditedShipping { get; set; }
        public string ItemNote { get; set; }
        public string FulfillmentCenterId { get; set; }
        public string MerchantShippingGroupName { get; set; }
        public string ProductTaxCode { get; set; }
        public int HandlingTime { get; set; }
        public bool BatteriesRequired { get; set; }
        public bool AreBatteriesIncluded { get; set; }
        public string BatteryCellComposition { get; set; }
        public string BatteryType { get; set; }
        public int NumberOfBatteries { get; set; }
        public decimal BatteryWeight { get; set; }
        public string BatteryWeightUnitOfMeasure { get; set; }
        public int NumberOfLithiumIonCells { get; set; }
        public int NumberOfLithiumMetalCells { get; set; }
        public string LithiumBatteryPackaging { get; set; }
        public decimal LithiumBatteryEnergyContent { get; set; }
        public string LithiumBatteryEnergyContentUnitOfMeasure { get; set; }
        public decimal LithiumBatteryWeight { get; set; }
        public string LithiumBatteryWeightUnitOfMeasure { get; set; }
        public string SupplierDeclaredDgHzRegulation1 { get; set; }
        public string SupplierDeclaredDgHzRegulation2 { get; set; }
        public string SupplierDeclaredDgHzRegulation3 { get; set; }
        public string SupplierDeclaredDgHzRegulation4 { get; set; }
        public string SupplierDeclaredDgHzRegulation5 { get; set; }
        public string HazmatUnitedNationsRegulatoryId { get; set; }
        public string SafetyDataSheetUrl { get; set; }
        public decimal ItemWeight { get; set; }
        public string ItemWeightUnitOfMeasure { get; set; }
        public decimal ItemVolume { get; set; }
        public string ItemVolumeUnitOfMeasure { get; set; }
        public decimal FlashPoint { get; set; }
        public string GhsClassificationClass1 { get; set; }
        public string GhsClassificationClass2 { get; set; }
        public string GhsClassificationClass3 { get; set; }
        public decimal ListPriceWithTax { get; set; }
        public decimal UvpListPrice { get; set; }

        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ProductTemplate(string path)
        {
            Path = path;
            ProductName = System.IO.Path.GetFileNameWithoutExtension(path);
            Sku = Regex.Replace(ProductName.ToUpper(), @"\s+", "-");


            foreach (var image in Directory.GetFiles(path, "*.*"))
            {
                Images.Add(new ProductImage(image));
            }
        }

        public ProductTemplate()
        { }
    }
}
