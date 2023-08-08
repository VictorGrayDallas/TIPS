using TIPS;
using TIPS.ViewModels;

namespace TIPSTestProject
{
	[TestClass]
	public class TestExpenseEditorModel : BasicAssert
	{
		private class TestUI : ExpenseEditorModel.ExpenseEditorUI
		{
			public bool isClosed = false;
			public void Close() => isClosed = true;

			public List<string> tags { private get; set; } = new();
			public List<string> GetTags() => tags;

			public bool deleteButtonHidden = false;
			public void HideDeleteButton() => deleteButtonHidden = true;
		}

		[TestMethod]
		public void TestOpeningNewExpense()
		{
			TestUI ui = new();
			ExpenseEditorModel model = new(null, ui, TestPlatformService.Instance);
			assert(model.EditedExpense != null);
			assert(model.EditedExpense!.Tags.Count == 0);
			assert(ui.deleteButtonHidden);
		}

		[TestMethod]
		public void TestEditingNewExpense()
		{
			TestUI ui = new();
			ExpenseEditorModel model = new(null, ui, TestPlatformService.Instance);
			model.EditedExpense!.Amount = 2.5m;
			model.EditedExpense.Date = new DateOnly(2020, 06, 09);
			model.EditedExpense.Description = "test";
			ui.tags = new List<string>()
			{
				"tag1",
				"tag2",
			};

			model.SaveClicked();
			assert(model.Result == PageResult.SAVE);
			assert(ui.isClosed);
			assert(model.EditedExpense.Amount == 2.5m);
			assert(model.EditedExpense.Date == new DateOnly(2020, 06, 09));
			assert(model.EditedExpense.Description == "test");
			assert(model.EditedExpense.Tags.Count == 2);
			assert(model.EditedExpense.Tags[0] == "tag1");
			assert(model.EditedExpense.Tags[1] == "tag2");
		}

		[TestMethod]
		public void TestOpeningExistingExpense()
		{
			TestUI ui = new();
			Expense expense = new Expense(new DateOnly(2020, 1, 2))
			{
				Amount = 5m,
				Description = "toy",
				Tags = new List<string>() { "fun things" },
			};
			ExpenseEditorModel model = new(expense, ui, TestPlatformService.Instance);
			assert(!ui.deleteButtonHidden);
			assert(model.EditedExpense != null);
			TIPSAssert.AssertExpensesMatch(expense, model.EditedExpense!);
		}

		[TestMethod]
		public void TestEditingExistingExpense()
		{
			TestUI ui = new();
			Expense expense = new Expense(new DateOnly(2020, 1, 2))
			{
				Amount = 5m,
				Description = "toy",
				Tags = new List<string>() { "fun things" },
			};
			ExpenseEditorModel model = new(expense, ui, TestPlatformService.Instance);
			model.EditedExpense!.Amount = 2.5m;
			model.EditedExpense.Date = new DateOnly(2020, 06, 09);
			model.EditedExpense.Description = "test";
			ui.tags = new List<string>()
			{
				"tag1",
				"tag2",
			};

			model.SaveClicked();
			assert(model.Result == PageResult.SAVE);
			assert(ui.isClosed);
			assert(model.EditedExpense.Amount == 2.5m);
			assert(model.EditedExpense.Date == new DateOnly(2020, 06, 09));
			assert(model.EditedExpense.Description == "test");
			assert(model.EditedExpense.Tags.Count == 2);
			assert(model.EditedExpense.Tags[0] == "tag1");
			assert(model.EditedExpense.Tags[1] == "tag2");
			// The edited expense should be the same instance that was passed in.
			assert(model.EditedExpense == expense);
		}

		[TestMethod]
		public void TestCancelingExistingExpense()
		{
			TestUI ui = new();
			Expense expense = new Expense(new DateOnly(2020, 1, 2))
			{
				Amount = 5m,
				Description = "toy",
				Tags = new List<string>() { "fun things" },
			};
			Expense original = new Expense(new DateOnly());
			original.CopyFrom(expense);

			ExpenseEditorModel model = new(expense, ui, TestPlatformService.Instance);
			model.EditedExpense!.Amount = 2.5m;
			model.EditedExpense.Date = new DateOnly(2020, 06, 09);
			model.EditedExpense.Description = "test";
			ui.tags = new List<string>()
			{
				"tag1",
				"tag2",
			};

			model.CancelClicked();
			assert(model.Result == PageResult.CANCEL);
			// The EditedExpense doesn't matter, but we need to make sure the instance passed in hasn't changed.
			TIPSAssert.AssertExpensesMatch(expense, original);
		}

		[TestMethod]
		public void TestDeleteExistingExpense()
		{
			TestUI ui = new();
			Expense expense = new Expense(new DateOnly(2020, 1, 2))
			{
				Amount = 5m,
				Description = "toy",
				Tags = new List<string>() { "fun things" },
			};
			Expense original = new Expense(new DateOnly());
			original.CopyFrom(expense);

			ExpenseEditorModel model = new(expense, ui, TestPlatformService.Instance);
			model.EditedExpense!.Amount = 2.5m;
			model.EditedExpense.Date = new DateOnly(2020, 06, 09);
			model.EditedExpense.Description = "test";
			ui.tags = new List<string>()
			{
				"tag1",
				"tag2",
			};

			model.DeleteClicked();
			assert(model.Result == PageResult.DELETE);
			// We don't care about properties, but the EditedExpense object needs to be the same instance that was passed in.
			assert(expense == model.EditedExpense);
		}

	}

}
