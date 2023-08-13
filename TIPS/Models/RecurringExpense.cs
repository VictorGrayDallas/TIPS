﻿using System;
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
	}
}