using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIPS.SQLite;

namespace TIPS
{
	internal class SQLiteService
	{
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

			Initialized = true;
		}


		#region "tags"
		private Dictionary<string, int>? AllTags;
		private static Dictionary<string, int> _TagIDsForBlobBuilding = new();
		private static Dictionary<int, string> _TagNamesForBlobParsing = new();
		/// <summary>
		/// Intended for internal use by SQLite types only.
		/// </summary>
		internal static int _TagId(string tagName) => _TagIDsForBlobBuilding[tagName];
		/// <summary>
		/// Intended for internal use by SQLite types only.
		/// </summary>
		internal static string _TagName(int tagId) => _TagNamesForBlobParsing[tagId];


		private async Task LoadTags()
		{
			var dbTags = await _db!.Table<SQLiteTag>().ToListAsync();
			AllTags = new();
			foreach (SQLiteTag tag in dbTags)
				AllTags[tag.TagName] = tag.Id;
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
					AllTags.Remove(tagName);
			}

			return true;
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
			_TagNamesForBlobParsing = new();
			foreach (KeyValuePair<string, int> kvp in AllTags!)
				_TagNamesForBlobParsing[kvp.Value] = kvp.Key;

			DateTime begin = beginInclusive == null ? DateTime.MinValue :
				beginInclusive.Value.ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);
			DateTime end = endInclusive == null ? DateTime.MaxValue :
				endInclusive.Value.AddDays(1).ToDateTime(new TimeOnly(0, 0), DateTimeKind.Utc);

			var dbExpenses = await _db!.Table<SQLiteExpense>()
				.Where((e) => e.Date >= begin && e.Date < end)
				.ToListAsync();
			return dbExpenses;
		}

		private async Task<SQLiteExpense> Insert(SQLiteExpense expense)
		{
			// SQLite will access the Tags property. Since SQLite can't handle lists, we dynamically convert
			// it to a blob. The property's get/set methods need access to tag IDs.
			if (AllTags == null)
				await LoadTags();
			// Further, we need to ensure all tags are already added.
			foreach (string tag in (expense as Expense).Tags)
				await AddTag(tag);
			_TagIDsForBlobBuilding = AllTags!;

			// Now we can insert
			if (await _db!.InsertAsync(expense, typeof(SQLiteExpense)) == 1)
				return expense;
			else
				// I do not know if this can ever actually happen.
				throw new Exception("SQLite failed to insert, for unknown reason.");
		}

		/// <summary>
		/// Adds the expense. If it already exists, does nothing.
		/// </summary>
		/// <returns>Returns the SQLite expense that was added (or already existed).
		/// Use the returned Expense object for any future updates or deletes.</returns>
		public async Task<Expense> AddExpense(Expense expense)
		{
			if (!Initialized)
				await Init();

			if (expense is SQLiteExpense sqlExpense)
			{
				// If this expense already exists, SQLite will add a copy of it and change this instance's Id.
				// This is not what we want, since the caller will no longer have a reference to the original course.
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

			if (expense is SQLiteExpense sqlExpense)
				return await _db!.DeleteAsync<SQLiteExpense>(sqlExpense.Id) != 0;
			else
				throw new Exception("Attempted to delete an expense that isn't a SQLite database expense. Use an instance that was returned by a Get or Add method.");

		}
		#endregion
	}
}
