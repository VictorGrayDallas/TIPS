using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS
{
	internal class RecurringExpense : Expense
	{
		public enum FrequencyUnits
		{
			Days,
			Weeks,
			Months,
			Years,
		}

		public int Frequency { get; set; }
		public FrequencyUnits FrequencyUnit { get; set; }
	
		public RecurringExpense(DateOnly date, int frequncy, FrequencyUnits unit) : base(date)
		{
			Frequency = frequncy;
			FrequencyUnit = unit;
		}

		public static RecurringExpense Copy(RecurringExpense other)
		{
			return new RecurringExpense(other.Date, other.Frequency, other.FrequencyUnit)
			{
				Description = other.Description,
				Amount = other.Amount,
				Tags = other.Tags.ToList(),
			};
		}

		public void CopyFrom(RecurringExpense other)
		{
			Date = other.Date;
			Description = other.Description;
			Amount = other.Amount;
			Tags = other.Tags.ToList();
			Frequency = other.Frequency;
			FrequencyUnit = other.FrequencyUnit;
		}

		public void MoveToNextDate()
		{
			if (FrequencyUnit == FrequencyUnits.Days)
				Date = Date.AddDays(Frequency);
			else if (FrequencyUnit == FrequencyUnits.Weeks)
				Date = Date.AddDays(Frequency * 7);
			else if (FrequencyUnit == FrequencyUnits.Months)
				Date = Date.AddMonths(Frequency);
			else if (FrequencyUnit == FrequencyUnits.Years)
				Date = Date.AddYears(Frequency);
		}
	}
}
