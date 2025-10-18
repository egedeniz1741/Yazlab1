using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Yazlab1.Model;


namespace Yazlab1.Converters // Namespace'in tam olarak bu olduğundan emin olun
{
    // Sınıfın public olduğundan emin olun
    public class DerslikListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<Derslik> derslikList && derslikList.Any())
            {
                // Derslik kodlarını al ve "-" ile birleştir
                return string.Join("-", derslikList.Select(d => d.DerslikKodu));
            }
            return string.Empty; // Liste boşsa veya uygun tipte değilse boş döndür
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Bu yönde dönüşüme gerek yok
            throw new NotImplementedException();
        }
    }
}