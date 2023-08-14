using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace TIPS.ViewModels
{
    class RecurenceConverter : IValueConverter
    {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is RecurringExpense re)
			{
				string unit = re.FrequencyUnit.ToString().ToLower();
				if (re.Frequency == 1)
					unit = unit[..(unit.Length - 1)];
				return $"Recurrs every {re.Frequency} {unit}.";
			}
			else if (value is Expense || value == null)
				return "";
			else
				throw new Exception("RecurenceConverter expected an expense, did not get one.");
		}

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("No support for converting back in RecurenceConverter.");
		}
	}
}
