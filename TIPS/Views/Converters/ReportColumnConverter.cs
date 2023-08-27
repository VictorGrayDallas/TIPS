using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace TIPS.ViewModels
{
    internal class ReportColumnConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return "null";

			if (value is not ReportColumn col)
				throw new Exception("Expected ReportColumn in ReportColumnConverter.");

			if (targetType == typeof(string))
				return col.Header;
			else
				throw new Exception("Invalid requested type in ReportColumnConverter.");
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("ReportColumnConverter cannot convert back to ReportColumn.");
		}
	}
}
