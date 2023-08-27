using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal class DateOnlySourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not DateOnly sourceDate)
				throw new Exception("Expected DateOnly in DateOnlySourceConverter. Are you in debug mode?");

			if (targetType == typeof(DateTime))
				return sourceDate.ToDateTime(new TimeOnly(12, 0));
			else if (targetType == typeof(string))
				return sourceDate.ToShortDateString();
			else
				throw new Exception("Invalid requested type in DateOnlySourceConverter.");
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not DateTime targetDate)
				return new DateOnly(0, 0, 0);

			return DateOnly.FromDateTime(targetDate);
		}
	}
}
