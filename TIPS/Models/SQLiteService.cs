using Microsoft.Maui.Controls;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIPS.Models.SQLiteWrappers;
using TIPS.SQLite;

namespace TIPS
{

	public class SQLiteServiceException : Exception
	{
		public SQLiteServiceException(string message) : base(message) { }
	}

	internal class SQLiteService
	{
		internal const string ErrorMessageForIgnoredFields = "This property exists only as a means of dealing with reflection-based shenanagins in SQLite and MAUI bindings. Do not use it. Use the base object's property or use the Sql_ verison.";

		private string dbName;
		private SQLiteAsyncConnection? _db;

		private Task? initTask;
		public bool Initialized { get; private set; } = false;

		public SQLiteService(string databaseName, bool autoInit = true)
		{
			dbName = databaseName;
			if (autoInit)
				initTask = Init();
		}

		public async Task Close()
		{
			if (initTask != null)
			{
				if (initTask.Status != TaskStatus.Faulted)
					await initTask;
				initTask = null;
			}

			if (_db != null)
				await _db.CloseAsync();
			_db = null;
			Initialized = false;
		}

		public async Task Init()
		{
			if (initTask != null)
			{
				await initTask;
				initTask = null;
				return;
			}
			if (_db != null)
				return;

			string dbFilePath = dbName;
			_db = new SQLiteAsyncConnection(dbFilePath);

			await _db.CreateTableAsync<SQLiteTag>();
			await _db.CreateTableAsync<SQLiteExpense>();
			await _db.CreateTableAsync<SQLiteRecurringExpense>();

			Initialized = true;
		}


		#region "tags"
		private Dictionary<string, int>? AllTags;
		private Dictionary<int, string> tagNamesForBlobParsing = new();

		private async Task LoadTags()
		{
			var dbTags = await _db!.Table<SQLiteTag>().ToListAsync();
			AllTags = new();
			foreach (SQLiteTag tag in dbTags)
				AllTags[tag.TagName] = tag.Id;

			tagNamesForBlobParsing = new();
			foreach (KeyValuePair<string, int> kvp in AllTags!)
				tagNamesForBlobParsing[kvp.Value] = kvp.Key;
		}
		public async Task<IEnumerable<string>> GetAllTags()
		{
			if (!Initialized)
				await Init();
			if (AllTags == null)
				await LoadTags();

			return AllTags!.Keys;
		}

		public async Task<bool> AddTag(string tagName)
		{
			if (!Initialized)
				await Init();
			if (AllTags == null)
				await GetAllTags();

			if (AllTags!.ContainsKey(tagName))
				return true;

			SQLiteTag sTag = new(tagName);
			if (await _db!.InsertAsync(sTag, typeof(SQLiteTag)) == 1)
			{
				AllTags!.Add(tagName, sTag.Id);
				tagNamesForBlobParsing.Add(sTag.Id, tagName);
				return true;
			}
			else
				return false;
		}

		public async Task<bool> DeleteTag(string tagName)
		{
			if (!Initialized)
				await Init();
			if (AllTags == null)
				await GetAllTags();

			if (AllTags!.ContainsKey(tagName))
			{
				if (await _db!.DeleteAsync<SQLiteTag>(AllTags[tagName]) == 0)
					return false;
				else
				{
					tagNamesForBlobParsing.Remove(AllTags[tagName]);
					AllTags.Remove(tagName);
				}
			}

			return true;
		}

		public async Task<bool> RenameTag(string oldName, string newName)
		{
			if (!Initialized)
				await Init();
			if (!AllTags!.ContainsKey(oldName))
				return false;
			if (oldName == newName)
				return true;

			SQLiteTag sTag = new(newName);
			sTag.Id = AllTags[oldName];		
			bool result = await _db!.UpdateAsync(sTag) == 1;

			if (result)
			{
				tagNamesForBlobParsing.Remove(AllTags[oldName]);
				AllTags.Remove(oldName);
				AllTags.Add(newName, sTag.Id);
				tagNamesForBlobParsing.Add(sTag.Id, newName);
			}
			return result;
		}
		#endregion

		#region "expenses"
		public async Task<IEnumerable<Expense>> GetExpenses(DateOnly? beginInclusive = null, DateOnly? endInclusive = null)
		{
			if (!Initialized)
				await Init();
			// SQLite will set the Tags property. Since SQLite can't handle lists, we use a blob which must
			// be converted back to a list. The property's get/set methods need access to tag IDs.
			if (AllTags == null)
				await LoadTags();

			DateTime begin = beginInclusive == null ? DateTime.MinValue :
				beginInclusive.Value.ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);
			DateTime end = endInclusive == null ? DateTime.MaxValue :
				endInclusive.Value.AddDays(1).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);

			var dbExpenses = await _db!.Table<SQLiteExpense>()
				.Where((e) => e.Sql_Date >= begin && e.Sql_Date < end)
				.ToListAsync();
			dbExpenses.ForEach((e) => ((ISQLiteExpense)e).ReceiveData(tagNamesForBlobParsing));
			return dbExpenses;
		}

		private async Task EnsureTagsExist(Expense expense)
		{
			if (AllTags == null)
				await LoadTags();
			foreach (string tag in expense.Tags)
				await AddTag(tag);
		}
		private async Task<Expense> Insert(Expense expense)
		{
			// SQLite will access the Tags property. Since SQLite can't handle lists, we dynamically convert
			// it to a blob. The property's get/set methods need access to tag IDs.
			await EnsureTagsExist(expense);
			(expense as ISQLiteExpense)!.ReceiveData(AllTags!);

			// Now we can insert
			Type t = expense is SQLiteExpense ? typeof(SQLiteExpense) : typeof(SQLiteRecurringExpense);
			if (await _db!.InsertAsync(expense, t) == 1)
				return expense;
			else
				// I do not know if this can ever actually happen.
				throw new SQLiteServiceException("SQLite failed to insert, for unknown reason.");
		}

		/// <summary>
		/// Adds the expense. If it already exists, does nothing.
		/// </summary>
		/// <returns>Returns the SQLite expense that was added (or already existed).
		/// Use the returned Expense object for any future updates or deletes.</returns>
		public async Task<Expense> AddSingleExpense(Expense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is SQLiteExpense sqlExpense)
			{
				// If this expense already exists, SQLite will add a copy of it and change this instance's Id.
				// This is not what we want, since the caller will no longer have a reference to the original expense.
				SQLiteExpense? existingExpense = await _db!.FindAsync<SQLiteExpense>(sqlExpense.Id);
				return existingExpense ?? await Insert(sqlExpense);
			}
			else
			{
				SQLiteExpense newExpense = new SQLiteExpense(expense);
				return await Insert(newExpense);
			}

		}

		public async Task<bool> DeleteExpense(Expense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is RecurringExpense re)
				return await DeleteRecurringExpense(re);
			else
				return await DeleteSingleExpense(expense);
		}
		private async Task<bool> DeleteSingleExpense(Expense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is SQLiteExpense sqlExpense)
				return await _db!.DeleteAsync<SQLiteExpense>(sqlExpense.Id) == 1;
			else
				throw new SQLiteServiceException("Attempted to delete an expense that isn't a SQLite database expense. Use an instance that was returned by a Get or Add method.");

		}

		public async Task<bool> UpdateExpense(Expense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is RecurringExpense re)
				return await UpdateRecurringExpense(re);
			else
				return await UpdateSingleExpense(expense);
		}
		private async Task<bool> UpdateSingleExpense(Expense expense)
		{
			if (expense is SQLiteExpense sqlExpense)
			{
				await EnsureTagsExist(expense);
				((ISQLiteExpense)sqlExpense).ReceiveData(AllTags!);
				return await _db!.UpdateAsync(sqlExpense) == 1;
			}
			else
				throw new SQLiteServiceException("Attempted to update an expense that isn't a SQLite database expense. Use an instance that was returned by a Get or Add method.");
		}
		#endregion

		#region "recurring expenses"
		public async Task<IEnumerable<RecurringExpense>> GetRecurringExpenses(bool onlyPastDue = false)
		{
			if (!Initialized)
				await Init();
			// SQLite will set the Tags property. Since SQLite can't handle lists, we use a blob which must
			// be converted back to a list. The property's get/set methods need access to tag IDs.
			if (AllTags == null)
				await LoadTags();

			var query = _db!.Table<SQLiteRecurringExpense>();
			if (onlyPastDue)
			{
				// We have to get a DateTime (not DateOnly) object for SQLite.
				// Make sure it's UTC (SQLite's timezome) and at the start of the next day.
				DateOnly endDateOnly = DateOnly.FromDateTime(DateTime.Today);
				DateTime end = endDateOnly.AddDays(1).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);
				query = query.Where((e) => e.Sql_Date < end);
			}

			var dbRecurringExpenses = await query.ToListAsync();
			dbRecurringExpenses.ForEach((e) => ((ISQLiteExpense)e).ReceiveData(tagNamesForBlobParsing));
			return dbRecurringExpenses;
		}

		/// <summary>
		/// Adds the expense. If it already exists, does nothing.
		/// </summary>
		/// <returns>Returns the SQLite expense that was added (or already existed).
		/// Use the returned Expense object for any future updates or deletes.</returns>
		public async Task<RecurringExpense> AddRecurringExpense(RecurringExpense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is SQLiteRecurringExpense sqlExpense)
			{
				// If this expense already exists, SQLite will add a copy of it and change this instance's Id.
				// This is not what we want, since the caller will no longer have a reference to the original course.
				SQLiteRecurringExpense? existingExpense = await _db!.FindAsync<SQLiteRecurringExpense>(sqlExpense.Id);
				return existingExpense ?? ((await Insert(sqlExpense)) as RecurringExpense)!;
			}
			else
			{
				SQLiteRecurringExpense newExpense = new SQLiteRecurringExpense(expense);
				return ((await Insert(newExpense)) as RecurringExpense)!;
			}

		}

		private async Task<bool> DeleteRecurringExpense(RecurringExpense expense)
		{
			if (expense is SQLiteRecurringExpense sqlExpense)
				return await _db!.DeleteAsync<SQLiteRecurringExpense>(sqlExpense.Id) == 1;
			else
				throw new SQLiteServiceException("Attempted to delete a recurring expense that isn't a SQLite database recurring expense. Use an instance that was returned by a Get or Add method.");
		}

		private async Task<bool> UpdateRecurringExpense(RecurringExpense expense)
		{
			if (expense is SQLiteRecurringExpense sqlExpense)
			{
				await EnsureTagsExist(expense);
				((ISQLiteExpense)sqlExpense).ReceiveData(AllTags!);
				return await _db!.UpdateAsync(sqlExpense) == 1;
			}
			else
				throw new SQLiteServiceException("Attempted to update a recurring expense that isn't a SQLite database recurring expense. Use an instance that was returned by a Get or Add method.");
		}

		public async Task ProcessRecurringExpense(RecurringExpense expense)
		{
			List<Task> tasks = new();
			while (expense.Date <= DateOnly.FromDateTime(DateTime.Today))
			{
				Expense concrete = new Expense(expense.Date);
				concrete.CopyFrom(expense);
				tasks.Add(AddSingleExpense(concrete));
				expense.MoveToNextDate();
			}
			tasks.Add(UpdateRecurringExpense(expense));
			foreach (Task t in tasks)
				await t;
		}
		public async Task ProcessRecurringExpenses()
		{
			List<Task> tasks = new();

			IEnumerable<RecurringExpense> recurringExpenses = await GetRecurringExpenses(true);
			foreach (RecurringExpense re in recurringExpenses)
				tasks.Add(ProcessRecurringExpense(re));

			foreach (Task t in tasks)
				await t;
		}
		#endregion
	}
}
