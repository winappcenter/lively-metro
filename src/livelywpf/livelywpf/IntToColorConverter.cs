using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace livelywpf
{
    [ValueConversion(typeof(ModernWpf.ApplicationTheme), typeof(SolidColorBrush))]
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ModernWpf.ApplicationTheme theme1 = (ModernWpf.ApplicationTheme)value;
            if (theme1 == ModernWpf.ApplicationTheme.Light)
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255,0,0,0));
            }
            else
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
