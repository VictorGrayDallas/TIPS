using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TIPS;

namespace TIPSTestProject
{
	internal static class TIPSAssert
	{
		public static void AssertExpensesMatch(Expense first, Expense second)
		{
			Assert.IsTrue(first is RecurringExpense == second is RecurringExpense);

			Assert.IsTrue(first.Amount == second.Amount);
			Assert.IsTrue(first.Description == second.Description);
			Assert.IsTrue(first.Date == second.Date);
			Assert.IsTrue(first.Tags.Count == second.Tags.Count);
			for (int i = 0; i < first.Tags.Count; i++)
				Assert.IsTrue(first.Tags[i] == second.Tags[i]);

			if (first is RecurringExpense fr && second is RecurringExpense sr)
			{
				Assert.IsTrue(fr.Frequency == sr.Frequency);
				Assert.IsTrue(fr.FrequencyUnit == sr.FrequencyUnit);
			}
		}
	}
}
