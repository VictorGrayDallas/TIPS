using Microsoft.Maui.Storage;
using System.Collections.Generic;
using System.IO;

using TIPS.ViewModels;

namespace TIPS.Views
{
	internal class DefaultPlatformService : PlatformServices
	{
		public string AppDataPath => FileSystem.AppDataDirectory;

		public string DefaultDatabaseName => "default.db";

		private Dictionary<string, SQLiteService> dbServices = new();
		public SQLiteService GetSQLiteService(string? filename = null)
		{
			if (filename == null)
				filename = Path.Combine(AppDataPath, DefaultDatabaseName);

			if (!dbServices.ContainsKey(filename))
				dbServices[filename] = new SQLiteService(filename);
				
			return dbServices[filename];
		}

		private DefaultPlatformService() { }
		public static DefaultPlatformService Instance = new();
	}
}
