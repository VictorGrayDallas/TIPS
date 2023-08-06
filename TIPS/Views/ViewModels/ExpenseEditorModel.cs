using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal class ExpenseEditorModel
	{
		public Expense? result;

		private PlatformServices platformServices;

		public ExpenseEditorModel(PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
		}

		public void SaveClicked()
		{
			result = new Expense(DateOnly.FromDateTime(DateTime.Now))
			{
				Amount = 1.23m,
				Description = "default",
			};

			string filename = Path.Combine(platformServices.AppDataPath, platformServices.DefaultDatabaseName);
			_ = platformServices.GetSQLiteService(filename).AddExpense(result);
		}
	}
}
