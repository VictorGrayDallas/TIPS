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
				return new DateTime(0, 0, 0);

			return sourceDate.ToDateTime(new TimeOnly(12, 0));
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not DateTime targetDate)
				return new DateOnly(0, 0, 0);

			return DateOnly.FromDateTime(targetDate);
		}
	}
}
