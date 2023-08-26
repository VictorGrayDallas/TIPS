using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TIPS.ViewModels
{
    internal class ReportRowConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return "[no tags]";

			if (value is not List<string> tags)
				throw new Exception("Expected List<string> in ReportRowConverter.");

			if (targetType == typeof(string))
			{
				if (tags.Any())
					return string.Join(", ", tags);
				else
					return "[no tags]";
			}
			else
				throw new Exception("Invalid requested type in ReportRowConverter.");
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("ReportRowConverter cannot convert back to list of tags.");
		}
	}
}
