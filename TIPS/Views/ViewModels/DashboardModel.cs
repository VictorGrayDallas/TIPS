using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal class DashboardModel
	{
		public ObservableCollection<Expense> RecentExpenses { get; } = new();

		public bool inited = false;
		private bool initStarted = false;

		private PlatformServices platformServices;
		private SQLiteService service;
		

		public DashboardModel(PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;

			RecentExpenses = new();
			DateOnly today = DateOnly.FromDateTime(DateTime.Now);
			RecentExpenses.Add(new Expense(today) { Description = "[loading]" });

			service = null!; // This removes the warning that service is null. It's actually set in Init.
			_ = Init();
		}

		public async Task Init()
		{
			if (initStarted)
				return;
			initStarted = true;

			string filename = Path.Combine(platformServices.AppDataPath, platformServices.DefaultDatabaseName);
			service = platformServices.GetSQLiteService(filename);
			await RefreshRecents();

			inited = true;
		}

		public async Task RefreshRecents()
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Now);
			IEnumerable<Expense> recents = (await service.GetExpenses(today.AddDays(-7)))
				.OrderByDescending((e) => e.Date)
				.Take(3);
			RecentExpenses.Clear();
			foreach (Expense e in recents)
				RecentExpenses.Add(e);
		}
	}
}
