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

		public class FilterOptions
		{
			public decimal MinAmount = 0;
			public decimal MaxAmount = decimal.MaxValue;

			public DateOnly MinDate = DateOnly.MinValue;
			public DateOnly MaxDate = DateOnly.MaxValue;

			public string? TextFilter = null;

			public List<string> Tags = new();
		}

		private ExpensesViewerUI ui;
		private PlatformServices platformServices;

		private IEnumerable<Expense> expensesInDateRange = new List<Expense>();
		private FilterOptions filter;
		public ObservableCollection<Expense> ExpensesInView { get; set; } = new();
		private bool viewingRecurring;

		public IEnumerable<string> AllTags { get => DefaultPlatformService.Instance.GetSQLiteService().GetAllTags().Result; }

		private SQLiteService service;

		public ExpensesViewerModel(bool viewRecurring, ExpensesViewerUI ui, PlatformServices? platformServices = null)
		{
			this.ui = ui;
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			viewingRecurring = viewRecurring;

			service = this.platformServices.GetSQLiteService();

			filter = new FilterOptions();
			filter.MinDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2));
			_ = RefreshList();
		}

		private async Task RefreshList()
		{
			await GetExpensesFromDatabse(filter);
			await Filter(filter);
		}

		public async void HandleNewExpense(ExpenseEditorModel editor)
		{
			if (editor.Result == PageResult.SAVE)
			{
				if (viewingRecurring)
				{
					await service.AddRecurringExpense(editor.ExpenseAsRecurring!);
					_ = service.ProcessRecurringExpense(editor.ExpenseAsRecurring!);
					_ = RefreshList();
				}
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
				if (editor.IsRecurring)
					_ = service.ProcessRecurringExpense(editor.ExpenseAsRecurring!);
				_ = RefreshList();
			}
			else if (editor.Result == PageResult.DELETE)
			{
				await service.DeleteExpense(editor.EditedExpense);
				_ = RefreshList();
			}
		}

		private async Task GetExpensesFromDatabse(FilterOptions options)
		{
			if (viewingRecurring)
				expensesInDateRange = await service.GetRecurringExpenses();
			else
				expensesInDateRange = await service.GetExpenses(options.MinDate, options.MaxDate);

		}
		public async Task Filter(FilterOptions? options)
		{
			IEnumerable<Expense> filtered;
			if (options == null)
			{
				options = new FilterOptions();
				if (filter.MaxDate != options.MaxDate || filter.MinDate != options.MinDate)
					await GetExpensesFromDatabse(options);
				filtered = expensesInDateRange;
			}
			else
			{
				if (filter.MaxDate != options.MaxDate || filter.MinDate != options.MinDate)
					await GetExpensesFromDatabse(options);

				filtered = expensesInDateRange
					.Where((e) => e.Amount >= options.MinAmount && e.Amount <= options.MaxAmount);
				if (!string.IsNullOrEmpty(options.TextFilter))
					filtered = filtered.Where((e) => e.Description.Contains(options.TextFilter));
				if (options.Tags.Any())
					filtered = filtered.Where((e) => {
						foreach (string t in options.Tags)
							if (e.Tags.Contains(t)) return true;
						return false;
					});
			}
			filtered = filtered.OrderByDescending((e) => e.Date);

			ExpensesInView.Clear();
			foreach (Expense e in filtered)
				ExpensesInView.Add(e);

			filter = options;
			ui.ListRefreshedHandler();
		}
	}
}
