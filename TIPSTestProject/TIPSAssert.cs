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
			Assert.IsTrue(first.Amount == second.Amount);
			Assert.IsTrue(first.Description == second.Description);
			Assert.IsTrue(first.Tags.Count == second.Tags.Count);
			for (int i = 0; i < first.Tags.Count; i++)
				Assert.IsTrue(first.Tags[i] == second.Tags[i]);
		}
	}
}
