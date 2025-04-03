using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SmartPhotShop.Converters
{
    public class StringToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., log the error)
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Implement if two-way binding is required
            throw new NotImplementedException();
        }
    }
}
