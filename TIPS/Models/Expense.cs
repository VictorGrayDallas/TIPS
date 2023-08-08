﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS
{
	public class Expense
	{
		public DateOnly Date { get; set; }
		public string Description { get; set; } = "";
		public decimal Amount { get; set; } = 0;
		public List<string> Tags { get; set; } = new();

		public Expense(DateOnly date)
		{
			Date = date;
		}

		public void CopyFrom(Expense other)
		{
			Date = other.Date;
			Description = other.Description;
			Amount = other.Amount;
			Tags = other.Tags.ToList();
		}
	}
}
