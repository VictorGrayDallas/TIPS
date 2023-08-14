﻿using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
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
		internal interface DashboardUI
		{
			void GetNewExpenseFromUser(Action<ExpenseEditorModel> callback);

			void GetEditedExpenseFromUser(Expense toEdit, Action<ExpenseEditorModel> callback);
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

			string filename = Path.Combine(platformServices.AppDataPath, platformServices.DefaultDatabaseName);
			service = platformServices.GetSQLiteService(filename);
			await RefreshRecents();

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

		public void NewExpenseClicked()
		{
			ui.GetNewExpenseFromUser(async (editor) => {
				if (editor.Result == PageResult.SAVE)
				{
					await service.AddSingleExpense(editor.EditedExpense);
					_ = RefreshRecents();
				}
			});
		}

		public void EditExpenseClicked(Expense toEdit)
		{
			ui.GetEditedExpenseFromUser(toEdit, async (editor) => {
				if (editor.Result == PageResult.SAVE)
				{
					await service.UpdateExpense(editor.EditedExpense);
					_ = RefreshRecents();
				}
				else if (editor.Result == PageResult.DELETE)
				{
					await service.DeleteExpense(editor.EditedExpense);
					_ = RefreshRecents();
				}
			});
		}
	}
}
