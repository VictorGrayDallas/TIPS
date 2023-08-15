using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIPS;
using TIPS.Models.SQLiteWrappers;
using TIPS.SQLite;

namespace TIPSTestProject
{
	[TestClass]
	public class TestSQLiteRecurringExpense : BasicAssert
	{
		// SQLiteRecurring expense has a lot of shenangigans going on.
		// This is a result of the fact that it needs to inherit from RecurringExpense (so user code can reference it as such without knowing anything about SQLite)
		// but at the same time it needs to have the SQLite-specific Date/Tags properties from SQLiteExpense.
		// Since we do not have multiple inheritance ... shenanigans.
		Dictionary<string, int> TagToId = new();

		public TestSQLiteRecurringExpense()
		{
			TagToId["tag"] = 0x123;
			TagToId["tag2"] = 4;
		}

		private RecurringExpense GetBasicTestExpense()
		{
			return new(DateOnly.FromDateTime(DateTime.Now), 1, RecurringExpense.FrequencyUnits.Days)
			{
				Amount = 1m,
				Description = "test",
				Tags = new List<string>() { "tag" },
			};
		}

		[TestMethod]
		public void TestConstructor()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);

			TIPSAssert.AssertExpensesMatch(baseExpense, sqlExpense);
		}

		private byte[] GetTagsBlob(SQLiteRecurringExpense sqlExpense)
		{
			(sqlExpense as ISQLiteExpense).ReceiveData(TagToId);
			return sqlExpense.Sql_Tags;
		}

		[TestMethod]
		public void TestTagsBlob()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);

			byte[] blob = GetTagsBlob(sqlExpense);
			assert(BitConverter.ToInt32(blob) == TagToId[baseExpense.Tags[0]]);
		}

		[TestMethod]
		public void TestTagsFromBlob()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);

			Dictionary<int, string> dict = new();
			dict[0x123] = "tag2";
			byte[] blob = new byte[] { 0x23, 1, 0, 0 };
			sqlExpense.Sql_Tags = blob;
			(sqlExpense as ISQLiteExpense).ReceiveData(dict);
			assert((sqlExpense as RecurringExpense).Tags[0] == "tag2");
			assert((sqlExpense as RecurringExpense).Tags == (sqlExpense as Expense).Tags);
		}

		[TestMethod]
		public void TestDatePropogatesToBase()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);
			baseExpense = sqlExpense;
			Expense se = baseExpense;

			sqlExpense.Sql_Date = DateTime.Now.AddDays(3);
			assert(baseExpense.Date == DateOnly.FromDateTime(sqlExpense.Sql_Date));
			assert(baseExpense.Date == se.Date);
		}

		[TestMethod]
		public void TestDatePropogatesFromBase()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);
			baseExpense = sqlExpense;

			baseExpense.Date = baseExpense.Date.AddDays(3);
			assert(baseExpense.Date == DateOnly.FromDateTime(sqlExpense.Sql_Date));
		}

		[TestMethod]
		public void TestTagsPropogatesFromBase()
		{
			RecurringExpense baseExpense = GetBasicTestExpense();
			SQLiteRecurringExpense sqlExpense = new(baseExpense);
			baseExpense = sqlExpense;

			baseExpense.Tags = new List<string>() { "tag2" };
			byte[] blob = GetTagsBlob(sqlExpense);
			assert(BitConverter.ToInt32(blob) == TagToId[baseExpense.Tags[0]]);
		}

	}
}
