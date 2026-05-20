using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Core
{
    public class WeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int totalGrams)
            {
                int kg = totalGrams / 1000;
                int g = totalGrams % 1000;

                if (kg > 0)
                {
                    return $"{kg}kg si {g}g";
                }
                return $"{g}g";
            }
            if (value is string strValue && int.TryParse(strValue, out int grams))
            {
                int kg = grams / 1000;
                int g = grams % 1000;
                if (kg > 0) return $"{kg}kg si {g}g";
                return $"{g}g";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
