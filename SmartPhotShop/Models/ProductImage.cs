using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartPhotShop.Models
{
    public class ProductImage : IEquatable<ProductImage>
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public bool Equals(ProductImage other)
        {
            if (other is null)
                return false;

            // Consider Name and Path to determine equality
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProductImage);
        }

        public override int GetHashCode()
        {
            // Combine Name and Path into the hash code
            return HashCode.Combine(
                Name?.ToLowerInvariant(),
                Path?.ToLowerInvariant()
            );
        }

        public static bool operator ==(ProductImage left, ProductImage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProductImage left, ProductImage right)
        {
            return !Equals(left, right);
        }
    }
}
