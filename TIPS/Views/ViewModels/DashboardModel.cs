using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TIPS.ViewModels
{
	internal class DashboardModel
	{
		internal interface DashboardUI
		{
		}

		public ObservableCollection<Expense> RecentExpenses { get; } = new();

		public bool inited = false;
		private bool initStarted = false;

		private PlatformServices platformServices;
		private DashboardUI ui;
		private SQLiteService service;
		

		public DashboardModel(DashboardUI ui, PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			this.ui = ui;

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

			service = platformServices.GetSQLiteService();

			await service.ProcessRecurringExpenses();
			_ = RefreshRecents();

			// Set up timer to automatically process recurring expenses every day.
			DateTime tomorrow = DateTime.Now.AddDays(1).Date;
			Timer timer = new Timer(tomorrow.AddSeconds(1).Subtract(DateTime.Now));
			timer.Enabled = true;
			timer.Elapsed += async (s, e) =>
			{
				DateTime tomorrow = DateTime.Now.AddDays(1).Date;
				(s as Timer)!.Interval = tomorrow.AddSeconds(1).Subtract(DateTime.Now).TotalMicroseconds;
				await service.ProcessRecurringExpenses();
				_ = RefreshRecents();
			};

			inited = true;
		}

		public async Task RefreshRecents()
		{
			DateOnly today = DateOnly.FromDateTime(DateTime.Now);
			IEnumerable<Expense> recents = (await service.GetExpenses(today.AddDays(-70)))
				.OrderByDescending((e) => e.Date)
				.Take(3);
			RecentExpenses.Clear();
			foreach (Expense e in recents)
				RecentExpenses.Add(e);
		}

		public async void HandleEditedExpense(ExpenseEditorModel editor)
		{
			if (editor.Result == PageResult.SAVE)
			{
				await service.UpdateExpense(editor.EditedExpense);
				if (editor.IsRecurring)
					_ = service.ProcessRecurringExpense(editor.ExpenseAsRecurring!);
				_ = RefreshRecents();
			}
			else if (editor.Result == PageResult.DELETE)
			{
				await service.DeleteExpense(editor.EditedExpense);
				_ = RefreshRecents();
			}
		}

		public async void HandleNewExpense(ExpenseEditorModel editor)
		{
			if (editor.Result == PageResult.SAVE)
			{
				await service.AddSingleExpense(editor.EditedExpense);
				_ = RefreshRecents();
			}
		}
	}
}
