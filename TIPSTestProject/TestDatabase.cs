using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TIPS;

namespace TIPSTestProject
{
	[TestClass]
	public class TestDatabase : BasicAssert
	{
		private string testDbName = Path.Combine(TestPlatformService.Instance.AppDataPath, TestPlatformService.Instance.DefaultDatabaseName);
		private SQLiteService service;

		// Constructor runs once for EACH test.
		public TestDatabase()
		{
			if (File.Exists(testDbName))
				File.Delete(testDbName);
			service = new(testDbName);
		}

		// Runs once for EACH test.
		[TestCleanup]
		public void Cleanup()
		{
			service.Close().Wait();
			File.Delete(testDbName);
		}

		[TestMethod]
		public async Task TestAddingTags()
		{
			Task t1 = service.AddTag("add1");
			Task t2 = service.AddTag("add2");
			await t1; await t2;

			HashSet<string> tags = (await service.GetAllTags()).ToHashSet();
			assert(tags.Contains("add1"));
			assert(tags.Contains("add2"));
		}

		[TestMethod]
		public async Task TestReaddingTag()
		{
			await service.AddTag("readd");
			
			IEnumerable<string> tags = await service.GetAllTags();
			assert(tags.Contains("readd"));
			int tagCount = tags.Count();

			await service.AddTag("readd");
			tags = await service.GetAllTags();
			assert(tags.Count() == tagCount);
		}

		[TestMethod]
		public async Task TestDeletingTags()
		{
			await service.AddTag("delete");

			HashSet<string> tags = (await service.GetAllTags()).ToHashSet();
			assert(tags.Contains("delete"));

			await service.DeleteTag("delete");
			tags = (await service.GetAllTags()).ToHashSet();
			assert(!tags.Contains("delete"));
		}

		[TestMethod]
		public async Task TestRenameTag()
		{
			await service.AddTag("tagToRename");
			HashSet<string> tags = (await service.GetAllTags()).ToHashSet();
			assert(tags.Contains("tagToRename"));

			await service.RenameTag("tagToRename", "newName");
			tags = (await service.GetAllTags()).ToHashSet();
			assert(!tags.Contains("tagToRename"));
			assert(tags.Contains("newName"));
		}

		[TestMethod]
		public async Task TestAddingDeletingExpense()
		{
			Expense expense = new Expense(new DateOnly(2016, 4, 9));
			expense.Amount = 5m;
			expense.Description = "testing";
			expense.Tags.Add("tag1");
			expense.Tags.Add("tag2");
			await service.AddExpense(expense);

			IEnumerable<Expense>  expenses = await service.GetExpenses();
			assert(expenses.Count() == 1);
			Expense returnedExpense = expenses.First();
			TIPSAssert.AssertExpensesMatch(returnedExpense, expense);

			await service.DeleteExpense(returnedExpense);
			expenses = await service.GetExpenses();
			assert(!expenses.Any());
		}

		[TestMethod]
		public async Task TestUpdatingExpense()
		{
			// Create and add
			Expense expense = new Expense(new DateOnly(2016, 4, 9));
			expense.Amount = 5m;
			expense.Description = "testing";
			expense.Tags.Add("tag1");
			expense.Tags.Add("tag2");
			expense = await service.AddExpense(expense);

			// Modify and update
			expense.Description = "tested";
			expense.Amount = 8m;
			expense.Tags.RemoveAt(0);
			await service.UpdateExpense(expense);

			// Get from DB and verify
			IEnumerable<Expense> expenses = await service.GetExpenses();
			assert(expenses.Count() == 1);
			Expense returnedExpense = expenses.First();
			TIPSAssert.AssertExpensesMatch(returnedExpense, expense);
		}

		[TestMethod]
		public async Task TestAddingDeletingRecurringExpense()
		{
			RecurringExpense expense = new RecurringExpense(new DateOnly(2016, 4, 9), 5, RecurringExpense.FrequencyUnits.Days);
			expense.Amount = 5m;
			expense.Description = "testing";
			expense.Tags.Add("tag1");
			expense.Tags.Add("tag2");
			await service.AddRecurringExpense(expense);

			IEnumerable<RecurringExpense> expenses = await service.GetRecurringExpenses();
			assert(expenses.Count() == 1);
			RecurringExpense returnedExpense = expenses.First();
			TIPSAssert.AssertExpensesMatch(returnedExpense, expense);

			await service.DeleteExpense(returnedExpense);
			expenses = await service.GetRecurringExpenses();
			assert(!expenses.Any());
		}

		[TestMethod]
		public async Task TestUpdatingRecurringExpense()
		{
			// Create and add
			RecurringExpense expense = new RecurringExpense(new DateOnly(2016, 4, 9), 5, RecurringExpense.FrequencyUnits.Days);
			expense.Amount = 5m;
			expense.Description = "testing";
			expense.Tags.Add("tag1");
			expense.Tags.Add("tag2");
			expense = await service.AddRecurringExpense(expense);

			// Modify and update
			expense.Description = "tested";
			expense.Amount = 8m;
			expense.Tags.RemoveAt(0);
			await service.UpdateExpense(expense);

			// Get from DB and verify
			IEnumerable<RecurringExpense> expenses = await service.GetRecurringExpenses();
			assert(expenses.Count() == 1);
			RecurringExpense returnedExpense = expenses.First();
			TIPSAssert.AssertExpensesMatch(returnedExpense, expense);
		}

		[TestMethod]
		public async Task TestProcessRecurringExpenses()
		{
			RecurringExpense recurringExpense = new RecurringExpense(
				DateOnly.FromDateTime(DateTime.Today).AddDays(-4), 5, RecurringExpense.FrequencyUnits.Days);
			recurringExpense.Amount = 5m;
			recurringExpense.Description = "testing";
			recurringExpense.Tags.Add("tag1");
			recurringExpense.Tags.Add("tag2");
			Expense copyFromRecurringExpense = new Expense(new DateOnly());
			copyFromRecurringExpense.CopyFrom(recurringExpense);
			RecurringExpense originalRecurringExpenseData = new RecurringExpense(
				recurringExpense.Date, recurringExpense.Frequency, recurringExpense.FrequencyUnit);
			originalRecurringExpenseData.CopyFrom(recurringExpense);

			await service.AddRecurringExpense(recurringExpense);
			await service.ProcessRecurringExpenses();

			IEnumerable<Expense> expenses = await service.GetExpenses();
			assert(expenses.Count() == 1);
			Expense expense = expenses.First();
			TIPSAssert.AssertExpensesMatch(expense, copyFromRecurringExpense);

			IEnumerable<RecurringExpense> recurringExpenses = await service.GetRecurringExpenses();
			recurringExpense = recurringExpenses.First();
			originalRecurringExpenseData.MoveToNextDate();
			TIPSAssert.AssertExpensesMatch(recurringExpense, originalRecurringExpenseData);
		}

		[TestMethod]
		public async Task TestRecurringExpenseCanCreateMultipleExpenses()
		{
			RecurringExpense recurringExpense = new RecurringExpense(
				DateOnly.FromDateTime(DateTime.Today).AddDays(-5), 5, RecurringExpense.FrequencyUnits.Days);

			await service.AddRecurringExpense(recurringExpense);
			await service.ProcessRecurringExpenses();

			IEnumerable<Expense> expenses = await service.GetExpenses();
			assert(expenses.Count() == 2);
		}
	}
}
