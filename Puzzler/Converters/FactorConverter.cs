using System;
using System.Globalization;
using System.Windows.Data;

namespace Puzzler.Converters
{
	public class FactorConverter : IValueConverter
	{
		public double Factor { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double val)
			{
				return val * Factor;
			}
			return Binding.DoNothing;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double val)
			{
				return val / Factor;
			}
			return Binding.DoNothing;
		}
	}
}
