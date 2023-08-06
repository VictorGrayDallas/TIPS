﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using TIPS;
using System.ComponentModel.Design;

namespace TIPSTestProject
{
	[TestClass]
	public class TestDatabase : BasicAssert
	{
		private string testDbName = Path.Combine(Path.GetTempPath(), "test.db");
		private SQLiteService service;

		public TestDatabase()
		{
			service = new(testDbName);
		}

		private void AssertExpensesMatch(Expense first, Expense second)
		{
			assert(first.Amount == second.Amount);
			assert(first.Description == second.Description);
			assert(first.Tags.Count == second.Tags.Count);
			for (int i = 0; i < first.Tags.Count; i++)
				assert(first.Tags[i] == second.Tags[i]);
		}

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
			IEnumerable<Expense> expenses = await service.GetExpenses();
			assert(expenses.Count() == 0, "Another test left one or more expenses in the database.");

			Expense expense = new Expense(new DateOnly(2016, 4, 9));
			expense.Amount = 5m;
			expense.Description = "testing";
			expense.Tags.Add("tag1");
			expense.Tags.Add("tag2");
			await service.AddExpense(expense);

			expenses = await service.GetExpenses();
			assert(expenses.Count() == 1);
			Expense returnedExpense = expenses.First();
			AssertExpensesMatch(returnedExpense, expense);

			await service.DeleteExpense(returnedExpense);
			expenses = await service.GetExpenses();
			assert(expenses.Count() == 0);
		}

		[TestMethod]
		public async Task TestUpdatingExpense()
		{
			IEnumerable<Expense> expenses = await service.GetExpenses();
			assert(expenses.Count() == 0, "Another test left one or more expenses in the database.");

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
			expenses = await service.GetExpenses();
			assert(expenses.Count() == 1);
			Expense returnedExpense = expenses.First();
			AssertExpensesMatch(returnedExpense, expense);

			// Clean
			await service.DeleteExpense(returnedExpense);
			expenses = await service.GetExpenses();
			assert(expenses.Count() == 0);
		}
	}
}
