using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using TIPS.ViewModels;
using System.Linq;
using System.Text;

namespace TIPS.Views;

public partial class ExpensesViewer : ContentPage, ExpensesViewerModel.ExpensesViewerUI
{
	ExpensesViewerModel model;
	public event Action? Closing;

	public ExpensesViewer(bool viewRecurring)
	{
		InitializeComponent();

		minDateEntry.Date = DateTime.Today.AddMonths(-2);

		model = new ExpensesViewerModel(viewRecurring, this);
		BindingContext = model;

		tagFilterEntry.ItemsSource = model.AllTags;

		if (viewRecurring)
		{
			detailsNextOccureceLabel.IsVisible = true;
			titleLabel.Text = "Recurring expenses";
		}
	}

	public void Close()
	{
		Closing?.Invoke();
		Navigation.PopModalAsync();
	}

	private void expensesCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		expenseDetailsView.BindingContext = expensesCollectionView.SelectedItem as Expense;
		expenseDetailsView.IsVisible = expenseDetailsView.BindingContext != null;
	}

	private void newExpense_Clicked(object sender, EventArgs e)
	{
		ExpenseEditor editor = new ExpenseEditor(model.ViewingRecurring);
		editor.Closing += model.HandleNewExpense;
		_ = Navigation.PushModalAsync(editor);
	}

	private void editExpense_Clicked(object sender, EventArgs e)
	{
		ExpenseEditor editor = new ExpenseEditor((Expense)expenseDetailsView.BindingContext);
		editor.Closing += model.HandleEditedExpense;
		_ = Navigation.PushModalAsync(editor);
	}

	public void ListRefreshedHandler()
	{
		expensesCollectionView.SelectedItem = null;
		expensesCollectionView.EmptyView = "There are no expenses.";
	}

	private void filter_Clicked(object sender, EventArgs e)
	{
		filterGrid.IsVisible = !filterGrid.IsVisible;
		string showOrHide = filterGrid.IsVisible ? "Hide" : "Show";
		filterButton.Text = showOrHide + " filters";
	}

	private void applyFilters_Clicked(object sender, EventArgs e)
	{
		StringBuilder validationErrors = new StringBuilder();
		decimal maxAmount = maxAmountEntry.Value == 0m ? decimal.MaxValue : maxAmountEntry.Value;
		if (minAmountEntry.Value > maxAmount)
			validationErrors.Append("Min amount must be lower than max amount. Set max to $0 to disable upper limit.\n");
		if (minDateEntry.Date > maxDateEntry.Date)
			validationErrors.Append("Min date must be lower than max date.\n");

		if (validationErrors.Length != 0)
		{
			validationErrors.Length--; // remove newline
			DisplayAlert("Error", validationErrors.ToString(), "okay");
			return;
		}

		_ = model.Filter(new ExpensesViewerModel.FilterOptions()
		{
			MinAmount = minAmountEntry.Value,
			MaxAmount = maxAmount,
			MinDate = DateOnly.FromDateTime(minDateEntry.Date),
			MaxDate = DateOnly.FromDateTime(maxDateEntry.Date),
			TextFilter = string.IsNullOrEmpty(textFilterEntry.Text) ? null : textFilterEntry.Text,
			Tags = tagFilterEntry.Tags,
		});
	}
}