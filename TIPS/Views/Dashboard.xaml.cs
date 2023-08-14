using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class Dashboard : ContentPage, DashboardModel.DashboardUI
{
	private DashboardModel model;

	public Dashboard()
	{
		InitializeComponent();
		model = new(this);
		BindingContext = model;
	}

	private void viewRecentExpenses_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		expenseDetailsView.BindingContext = viewRecentExpenses.SelectedItem as Expense;
		expenseDetailsView.IsVisible = expenseDetailsView.BindingContext != null;
	}

	private void viewSingleExpensesButton_Clicked(object sender, EventArgs e) => ViewExpenses(false);
	private void viewRecurringExpensesButton_Clicked(object sender, EventArgs e) => ViewExpenses(true);
	private void ViewExpenses(bool viewRecurring)
	{
		ExpensesViewer viewer = new(viewRecurring);
		viewer.Closing += () => {
			_ = model.RefreshRecents();
		};
		_ = Navigation.PushModalAsync(viewer);

	}
}