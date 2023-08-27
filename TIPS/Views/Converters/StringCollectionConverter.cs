using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TIPS.ViewModels
{
	internal class StringCollectionConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not IEnumerable<string> sourceDate)
				return "[bad data]";

			return string.Join(", ", sourceDate);
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("No support for converting string to tag list.");
		}
	}
}
