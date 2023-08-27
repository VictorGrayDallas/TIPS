using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class Dashboard : ContentPage, DashboardModel.DashboardUI
{
	private DashboardModel model;
	private bool firstAppearance;
	private List<ReportView> reports;

	public Dashboard()
	{
		InitializeComponent();

		firstAppearance = true;
		model = new(this);
		BindingContext = model;

		reports = new();
		foreach (var rs in model.Reports)
		{
			ReportView report = new ReportView(rs);
			report.EditClicked += editReport_Clicked;
			reports.Add(report);
			reportsLayout.Add(report);
		}

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

	public void RecentsRefreshed()
	{
		recentActivityIndicator.IsRunning = recentActivityIndicator.IsVisible = false;
		viewRecentExpenses.IsVisible = true;
		viewRecentExpenses.EmptyView = "There are no recent expenses.";
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

	private void newExpense_Clicked(object sender, EventArgs e)
	{
		ExpenseEditor editor = new ExpenseEditor(false);
		editor.Closing += model.HandleNewExpense;
		_ = Navigation.PushModalAsync(editor);
	}

	private async void editReport_Clicked(ReportView sender)
	{
		sender.IsEnabled = false;
		pageActivityIndicator.IsRunning = true;
		// Actually draw the activity indicator befor loading the new page
		await Task.Yield();

		ReportEditor editor = new ReportEditor(sender.GetSettings().Clone());
		int index = reports.IndexOf(sender);
		editor.Closing += (r) =>
		{
			if (r.Result == PageResult.SAVE)
			{
				model.Reports[index] = r.EditedSettings;
				model.SaveReports();
				sender.UpdateSettings(r.EditedSettings);
				sender.RefreshData();
			}
			else if (r.Result == PageResult.DELETE)
			{
				model.Reports.RemoveAt(index);
				model.SaveReports();
				reports.RemoveAt(index);
				reportsLayout.RemoveAt(index);
			}

			pageActivityIndicator.IsRunning = false;
			sender.IsEnabled = true;
		};
		_ = Navigation.PushModalAsync(editor);

	}
}