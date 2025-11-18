// GameUI/Converters/ProductConverter.cs
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GameUI
{
    public class ProductConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Multiply all numeric inputs (e.g., Col \* CellSize)
            try
            {
                double product = 1.0;
                foreach (var v in values.Where(v => v != null))
                {
                    if (double.TryParse(v.ToString(), out var d))
                        product *= d;
                }
                return product;
            }
            catch { return 0.0; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}