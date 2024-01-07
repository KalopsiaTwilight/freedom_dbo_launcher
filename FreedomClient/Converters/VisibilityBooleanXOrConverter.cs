using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace FreedomClient.Converters
{
    public class VisibilityBooleanXOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var counter = 0;
            if (values.Length > 0)
            {
                if (values[0] is bool firstVal)
                {
                    if (!firstVal)
                        return Visibility.Hidden;
                } else
                {
                    return Visibility.Hidden;
                }
            }
            foreach (object value in values)
            {
                if ((value is bool val) && val)
                {
                    counter++;
                }
            }
            return counter > 1 ? Visibility.Hidden: Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException($"{nameof(BooleanOrConverter)} is a OneWay converter.");
        }
    }
}
