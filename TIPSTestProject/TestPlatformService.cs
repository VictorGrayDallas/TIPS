using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIPS;
using TIPS.ViewModels;

namespace TIPSTestProject
{
	internal class TestPlatformService : PlatformServices
	{
		public string AppDataPath => Path.GetTempPath();

		public string DefaultDatabaseName => "test.db";

		private SQLiteService defaultService;
		public SQLiteService GetSQLiteService(string? filename = null)
		{
			if (filename == null)
				return defaultService;
			else
				throw new NotImplementedException();
		}

		private TestPlatformService() {
			defaultService = new(Path.Combine(AppDataPath, DefaultDatabaseName));
		}
		public static TestPlatformService Instance = new();
	}
}
