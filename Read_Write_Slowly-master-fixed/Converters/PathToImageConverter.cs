using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Read_Write_Slowly.Converters
{
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = value as string;
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                // Если это pack:// URI (встроенный ресурс) — отдаём напрямую
                if (path.StartsWith("pack://"))
                {
                    return new BitmapImage(new Uri(path));
                }

                // Иначе — локальный файл
                if (!File.Exists(path)) return null;

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
