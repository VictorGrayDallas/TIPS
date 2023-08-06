using Microsoft.Maui.Controls;
using System;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class ExpenseEditor : ContentPage
{
	private ExpenseEditorModel model = new();

	public ExpenseEditor()
	{
		InitializeComponent();
	}

	private void saveExpense_Clicked(object sender, EventArgs e)
	{
		model.SaveClicked();
		Navigation.PopModalAsync();
	}

	private void cancelExpense_Clicked(object sender, EventArgs e)
	{

	}
}