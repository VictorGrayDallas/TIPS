using TIPS;

namespace TIPSTestProject
{
	[TestClass]
	public class TestExpense : BasicAssert
	{
		[TestMethod]
		public void TestConstructor()
		{
			Expense expense = new(new DateOnly(2023, 07, 01));
			assert(expense.Amount == 0m);
			assert(expense.Description == string.Empty);
			assert(expense.Tags.Count == 0);
			assert(expense.Date == new DateOnly(2023, 07, 01));
		}
	}
}