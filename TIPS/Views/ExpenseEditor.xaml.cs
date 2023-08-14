using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class ExpenseEditor : ContentPage, ExpenseEditorModel.ExpenseEditorUI
{
	internal ExpenseEditorModel model;

	public ExpenseEditor(Expense? toEdit)
	{
		InitializeComponent();

		model = new(toEdit, this);

		// Since our bindings are OneWayToSource, we have to manually set initial values.
		amountEntry.Text = model.EditedExpense.Amount.ToString("N2");
		dateEntry.Date = model.EditedExpense.Date.ToDateTime(new TimeOnly(12, 0));
		descriptionEntry.Text = model.EditedExpense.Description;
		tagEntry.ItemsSource = model.AllTags;
		tagEntry.SetTags(model.EditedExpense.Tags);
		BindingContext = model;

		// I cannot get this to work correctly. I tried using bindings in the XAML too.
		// The label appears in the wrong place when the page loads, but moves when I type in the tags box.
		//descriptionEntry.SizeChanged += (s, e) =>
		//{
		//	tagsLabel.HeightRequest = descriptionEntry.Height;
		//};
	}

	private void saveExpense_Clicked(object sender, EventArgs e) => model.SaveClicked();

	private void cancelExpense_Clicked(object sender, EventArgs e) => model.CancelClicked();

	public void HideDeleteButton() => saveCancelGrid.ColumnDefinitions.RemoveAt(2);

	public void Close()
	{
		Closing?.Invoke(model);
		Navigation.PopModalAsync();
	}

	public List<string> GetTags() => tagEntry.Tags;

	internal event Action<ExpenseEditorModel>? Closing;

	private void deleteExpense_Clicked(object sender, EventArgs e) => model.DeleteClicked();
}