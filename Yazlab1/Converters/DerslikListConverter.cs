using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Yazlab1.Model;


namespace Yazlab1.Converters 
{
    
    public class DerslikListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<Derslik> derslikList && derslikList.Any())
            {
               
                return string.Join("-", derslikList.Select(d => d.DerslikKodu));
            }
            return string.Empty; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
           
            throw new NotImplementedException();
        }
    }
}