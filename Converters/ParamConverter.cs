using System;
using System.Globalization;
using System.Windows.Data;

namespace Read_Write_Slowly.Converters
{
    public class ParamConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Просто возвращает массив объектов обратно во ViewModel
            return values.Clone();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
