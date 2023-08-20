using System.Runtime.CompilerServices;
using TIPS;
using TIPS.ViewModels;

namespace TIPSTestProject
{
	[TestClass]
	public class TestExpensesViewerModel : BasicAssert
	{
		internal class TestUI : ExpensesViewerModel.ExpensesViewerUI
		{
			public bool refreshed = false;

			public void ListRefreshedHandler() => refreshed = true;

			public async Task WaitUntilRefresh()
			{
				int count = 0;
				while (!refreshed && count < 100)
				{
					await Task.Delay(50);
					count++;
				}
				Assert.IsTrue(count < 100, "WaitUntilRefresh timed out.");

				refreshed = false;
			}
		}

		private static TestPlatformService service = null!; // Set in ClassInitialize method
		private static string testDbName = null!;

		private static List<Expense> expenses = new();
		private static List<RecurringExpense> recurringExpenses = new();

		// Just in case we hit midnight mid-test.
		private static DateOnly today = DateOnly.FromDateTime(DateTime.Now);

		public TestExpensesViewerModel() { }

		[ClassInitialize]
		public static async Task CreateExpensesToView(TestContext testContext)
		{
			service = new TestPlatformService("expensesviewermodel.db");
			testDbName = Path.Combine(service.AppDataPath, "expensesviewermodel.db");
			if (File.Exists(testDbName))
				File.Delete(testDbName);

			Expense expense1 = new Expense(today)
			{
				Amount = 2.25m,
				Description = "Testing things",
				Tags = new List<string>()
				{
					"tag1",
					"tag2",
				},
			};
			Expense expense2 = Expense.Copy(expense1);
			expense2.Description = "asdf";
			expense2.Date = expense2.Date.AddDays(1);
			expense2.Amount = 3m;
			Expense expense3 = Expense.Copy(expense1);
			expense3.Amount = 9.99m;
			expense3.Date = expense3.Date.AddMonths(-2);
			expense3.Tags.RemoveAt(1);

			RecurringExpense recurringExpense = new RecurringExpense(expense1.Date.AddDays(-4), 2, RecurringExpense.FrequencyUnits.Weeks);
			recurringExpense.CopyFrom(expense1);

			SQLiteService sqlService = service.GetSQLiteService();
			expenses.Add(await sqlService.AddSingleExpense(expense1));
			expenses.Add(await sqlService.AddSingleExpense(expense2));
			expenses.Add(await sqlService.AddSingleExpense(expense3));
			recurringExpenses.Add(await sqlService.AddRecurringExpense(recurringExpense));
		}
		[ClassCleanup]
		public static void Cleanup()
		{
			File.Delete(testDbName);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestExpensesAreInList()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 3);
			// Sorted by date
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], expenses[1]);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[1], expenses[0]);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[2], expenses[2]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestRecurringExpenseInList()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(true, ui, service);
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 1);
			// Sorted by date
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], recurringExpenses[0]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestEditingNewExpense()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			ExpenseEditorModel editorModel = new(false, new TestExpenseEditorModel.TestUI(), service);
			editorModel.EditedExpense.Amount = 9m;
			editorModel.EditedExpense.Date = DateOnly.FromDateTime(DateTime.Now.AddDays(14));
			editorModel.SaveClicked();
			model.HandleNewExpense(editorModel);
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 4);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], editorModel.EditedExpense);

			// Cleanup
			await service.GetSQLiteService().DeleteExpense(model.ExpensesInView[0]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestEditingExistingExpense()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			TestExpenseEditorModel.TestUI editorUI = new();
			ExpenseEditorModel editorModel = new(model.ExpensesInView[0], editorUI, service);
			editorUI.tags = editorModel.EditedExpense.Tags.ToList();
			editorModel.EditedExpense.Amount = 9m;
			editorModel.SaveClicked();
			model.HandleEditedExpense(editorModel);
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 3);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], editorModel.EditedExpense);

			// Cleanup
			editorModel.EditedExpense.Amount = expenses[1].Amount;
			await service.GetSQLiteService().UpdateExpense(editorModel.EditedExpense);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestDeleteExistingExpense()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			Expense original = model.ExpensesInView[0];
			ExpenseEditorModel editorModel = new(model.ExpensesInView[0], new TestExpenseEditorModel.TestUI(), service);
			editorModel.EditedExpense.Amount = 9m;
			editorModel.DeleteClicked();
			model.HandleEditedExpense(editorModel);
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 2);
			assert(!model.ExpensesInView.Contains(original));

			// Cleanup
			original.Amount = expenses[1].Amount;
			expenses[1] = await service.GetSQLiteService().AddSingleExpense(original);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestFilterDates()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			await model.Filter(new ExpensesViewerModel.FilterOptions()
			{
				MinDate = today.AddDays(-10),
				MaxDate = today,
			});
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 1);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], expenses[0]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestFilterAmounts()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			await model.Filter(new ExpensesViewerModel.FilterOptions()
			{
				MinAmount = 2.5m,
				MaxAmount = 5m,
			});
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 1);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], expenses[1]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestFilterDescription()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			await model.Filter(new ExpensesViewerModel.FilterOptions()
			{
				TextFilter = "asd",
			});
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 1);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], expenses[1]);
		}

		[TestMethod]
		[DoNotParallelize]
		public async Task TestFilterTags()
		{
			TestUI ui = new();
			ExpensesViewerModel model = new(false, ui, service);
			await ui.WaitUntilRefresh();

			await model.Filter(new ExpensesViewerModel.FilterOptions()
			{
				Tags = new List<string>() { "tag2" },
			});
			await ui.WaitUntilRefresh();

			assert(model.ExpensesInView.Count == 2);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[0], expenses[1]);
			TIPSAssert.AssertExpensesMatch(model.ExpensesInView[1], expenses[0]);
		}

	}
}
