using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal class ExpensesViewerModel
	{
		public interface ExpensesViewerUI
		{
			void ListRefreshedHandler();
		}

		private ExpensesViewerUI ui;
		private PlatformServices platformServices;

		public ObservableCollection<Expense> ExpensesInView { get; set; } = new();
		private bool viewingRecurring;

		private SQLiteService service;

		public ExpensesViewerModel(bool viewRecurring, ExpensesViewerUI ui, PlatformServices? platformServices = null)
		{
			this.ui = ui;
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			viewingRecurring = viewRecurring;

			service = this.platformServices.GetSQLiteService();

			_ = RefreshList();
		}

		private async Task RefreshList()
		{
			IEnumerable<Expense> expenses;
			if (viewingRecurring)
				expenses = await service.GetRecurringExpenses();
			else
				expenses = await service.GetExpenses();
			expenses.OrderByDescending((e) => e.Date);

			ExpensesInView.Clear();
			foreach (Expense e in expenses)
				ExpensesInView.Add(e);

			ui.ListRefreshedHandler();
		}

		public async void HandleNewExpense(ExpenseEditorModel editor)
		{
			if (editor.Result == PageResult.SAVE)
			{
				if (viewingRecurring)
					await service.AddRecurringExpense((RecurringExpense)editor.EditedExpense);
				else
					await service.AddSingleExpense(editor.EditedExpense);
				_ = RefreshList();
			}
		}

		public async void HandleEditedExpense(ExpenseEditorModel editor)
		{
			if (editor.Result == PageResult.SAVE)
			{
				await service.UpdateExpense(editor.EditedExpense);
				_ = RefreshList();
			}
			else if (editor.Result == PageResult.DELETE)
			{
				await service.DeleteExpense(editor.EditedExpense);
				_ = RefreshList();
			}
		}
	}
}
