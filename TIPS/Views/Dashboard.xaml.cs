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

		new Thread(_ =>
		{
			_ = model.Init();

			MainThread.BeginInvokeOnMainThread(() => testbutton.Text = "Init called");
		}).Start();
	}

	void DashboardModel.DashboardUI.GetNewExpenseFromUser(Action<ExpenseEditorModel> callback)
	{
		ExpenseEditor editor = new ExpenseEditor(null);
		editor.Closing += callback;
		_ = Navigation.PushModalAsync(editor);
	}

	void DashboardModel.DashboardUI.GetEditedExpenseFromUser(Expense toEdit, Action<ExpenseEditorModel> callback)
	{
		ExpenseEditor editor = new ExpenseEditor(toEdit);
		editor.Closing += callback;
		_ = Navigation.PushModalAsync(editor);
	}

	private void viewRecentExpenses_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		expenseDetailsView.BindingContext = viewRecentExpenses.SelectedItem as Expense;
		expenseDetailsView.IsVisible = expenseDetailsView.BindingContext != null;
	}

	private void testbutton_Clicked(object sender, EventArgs e)
	{
		_ = model.RefreshRecents();
	}

	private void newExpense_Clicked(object sender, EventArgs e) => model.NewExpenseClicked();

	private void editExpense_Clicked(object sender, EventArgs e) => model.EditExpenseClicked((Expense)expenseDetailsView.BindingContext);
}