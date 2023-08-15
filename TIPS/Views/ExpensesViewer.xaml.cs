using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using TIPS.ViewModels;
using System.Linq;

namespace TIPS.Views;

public partial class ExpensesViewer : ContentPage, ExpensesViewerModel.ExpensesViewerUI
{
	ExpensesViewerModel model;
	public event Action? Closing;

	public ExpensesViewer(bool viewRecurring)
	{
		InitializeComponent();

		model = new ExpensesViewerModel(viewRecurring, this);
		BindingContext = model;

		if (viewRecurring)
			detailsNextOccureceLabel.IsVisible = true;
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
		ExpenseEditor editor = new ExpenseEditor(true);
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

}