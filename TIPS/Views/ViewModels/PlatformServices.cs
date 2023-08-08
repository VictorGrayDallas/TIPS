using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal interface PlatformServices
	{
		string AppDataPath { get; }

		string DefaultDatabaseName { get; }

		SQLiteService GetSQLiteService(string? filename = null);
	}
}
