using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class Dashboard : ContentPage, DashboardModel.DashboardUI
{
	private DashboardModel model;
	private bool firstAppearance;

	public Dashboard()
	{
		InitializeComponent();

		firstAppearance = true;
		model = new(this);
		BindingContext = model;

		// It's so sad that there doesn't seem to be a way to do this in XAML.
		// We're making it so that the label in column 0 cannot squeeze column 1 smaller than its contents want to be.
		editButton.SizeChanged += (s, e) =>
		{
			expenseDetailsStack.MaximumWidthRequest = expenseDetailsGrid.Width - editButton.Width - expenseDetailsGrid.ColumnSpacing - editButton.Margin.Left - expenseDetailsStack.Margin.Right;
			expenseDetailsStack.WidthRequest = -1;
		};

		// https://github.com/dotnet/maui/issues/8798
		// It would seem that the developers of MAUI do not think that knowning the default font size is a good thing.
		// Why do they think that nothing other than font size should ever be scaled based on the system's default font size!?
		// The more I use MAUI the more I hate it.
		// I cannot come up with a satisfactory solution to set the recent expense description labal's MaximumWidthRequest
		// based on font size.
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (!firstAppearance)
			_ = model.RefreshRecents();
		firstAppearance = false;
	}

	private void viewRecentExpenses_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		expenseDetailsGrid.BindingContext = viewRecentExpenses.SelectedItem as Expense;
		expenseDetailsGrid.IsVisible = expenseDetailsGrid.BindingContext != null;
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

	private void editExpense_Clicked(object sender, EventArgs e)
	{
		ExpenseEditor editor = new ExpenseEditor((Expense)expenseDetailsGrid.BindingContext);
		editor.Closing += model.HandleEditedExpense;
		_ = Navigation.PushModalAsync(editor);
	}
}