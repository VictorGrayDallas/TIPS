using Microsoft.Maui.Storage;
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
		public async Task<IEnumerable<string>> GetAllTags()
		{
			if (!Initialized)
				await Init();
			if (AllTags == null)
			{
				var dbTags = await _db!.Table<SQLiteTag>().ToListAsync();
				AllTags = new();
				foreach (SQLiteTag tag in dbTags)
					AllTags[tag.TagName] = tag.Id;
			}

			return AllTags.Keys;
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

	}
}
