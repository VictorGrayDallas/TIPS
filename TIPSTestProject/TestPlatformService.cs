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
		public string AppDataPath => Path.Combine(Path.GetTempPath(), "TIPS");

		public string DefaultDatabaseName { get; private set; }

		private SQLiteService defaultService;
		public SQLiteService GetSQLiteService(string? filename = null)
		{
			if (filename == null)
				return defaultService;
			else
				throw new NotImplementedException();
		}

		public TestPlatformService(string dbName) {
			DefaultDatabaseName = dbName;
			defaultService = new(Path.Combine(AppDataPath, dbName));
		}
	}
}
