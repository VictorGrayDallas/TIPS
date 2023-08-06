using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class Dashboard : ContentPage
{
	private DashboardModel model = new();

	public Dashboard()
	{
		InitializeComponent();
		BindingContext = model;

		new Thread(_ =>
		{
			_ = model.Init();

			MainThread.BeginInvokeOnMainThread(() => testbutton.Text = "Init called");
		}).Start();
	}

	private void viewRecentExpenses_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void testbutton_Clicked(object sender, EventArgs e)
	{
		_ = model.RefreshRecents();
	}

	private void newExpense_Clicked(object sender, EventArgs e)
	{
		//Shell.Current.GoToAsync(nameof(ExpenseEditor));
		Navigation.PushModalAsync(new ExpenseEditor());
	}
}