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
		private SQLiteAsyncConnection? _db;

		private Task? initTask;
		public bool Initialized { get; private set; } = false;

		public SQLiteService(string databaseName, bool autoInit = true)
		{
			if (autoInit)
				initTask = Init(databaseName);
		}

		public async Task Init(string databaseName)
		{
			if (initTask != null)
			{
				await initTask;
				initTask = null;
				return;
			}
			if (_db != null)
				return;

			string dbFilePath = Path.Combine(FileSystem.AppDataDirectory, databaseName);
			_db = new SQLiteAsyncConnection(dbFilePath);

			await _db.CreateTableAsync<SQLiteTag>();
			await _db.CreateTableAsync<SQLiteExpense>();

			Initialized = true;
		}



	}
}
