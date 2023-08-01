using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS
{
	internal class Expense
	{
		public DateOnly Date { get; set; }
		public string Description { get; set; } = "";
		public decimal Amount { get; set; } = 0;
		public List<string> Tags { get; set; } = new();
	}
}
