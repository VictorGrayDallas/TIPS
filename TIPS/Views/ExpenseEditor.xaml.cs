using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class ExpenseEditor : ContentPage, ExpenseEditorModel.ExpenseEditorUI
{
	internal ExpenseEditorModel model;

	public ExpenseEditor(Expense toEdit)
	{
		InitializeComponent();

		model = new(toEdit, this);
		Init();
	}
	public ExpenseEditor(bool newExpenseIsRecurring)
	{
		InitializeComponent();

		model = new(newExpenseIsRecurring, this);
		Init();
	}

	private void Init()
	{ 
		// Since our bindings are OneWayToSource, we have to manually set initial values before assigning BindingContext.
		amountEntry.Text = model.EditedExpense.Amount.ToString("N2");
		dateEntry.Date = model.EditedExpense.Date.ToDateTime(new TimeOnly(12, 0));
		descriptionEntry.Text = model.EditedExpense.Description;
		tagEntry.ItemsSource = model.AllTags;
		tagEntry.SetTags(model.EditedExpense.Tags);

		if (model.IsRecurring)
		{
			dateLabel.Text = "Next date";
			frequencyEntry.Text = model.ExpenseAsRecurring!.Frequency.ToString();
			unitPicker.ItemsSource = new RecurringExpense.FrequencyUnits[]
			{
				RecurringExpense.FrequencyUnits.Days,
				RecurringExpense.FrequencyUnits.Weeks,
				RecurringExpense.FrequencyUnits.Months,
				RecurringExpense.FrequencyUnits.Years,
			};
			unitPicker.SelectedItem = model.ExpenseAsRecurring.FrequencyUnit;
		}
		else
		{
			frequencyEntry.IsEnabled = unitPicker.IsEnabled = false;
			grid.RowDefinitions[4].Height = 0;
		}

		BindingContext = model;

		// It's so sad that there doesn't seem to be a way to do this in XAML.
		triggerWarningLayout.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(HorizontalStackLayout.IsVisible))
			{
				// Initial estimates. Cannot reference actual sizes of not-yet-rendered views.
				triggerWarningLabel.WidthRequest = dateEntry.Width - tagsLabel.Height;
				triggerWarningImage.HeightRequest = dateEntry.Height;
			}
		};
		triggerWarningLabel.SizeChanged += (s, e) =>
		{
			// Real sizes. (Yes, it's possible this gets called multiple times.)
			triggerWarningLabel.WidthRequest = Math.Max(dateEntry.Width - triggerWarningImage.Width, triggerWarningLabel.MinimumWidthRequest);
			triggerWarningImage.HeightRequest = Math.Min(triggerWarningLabel.Height, triggerWarningImage.MaximumHeightRequest);
		};

		// I cannot get this to work correctly. I tried using bindings in the XAML too.
		// The label appears in the wrong place when the page loads, but moves when I type in the tags box.
		//descriptionEntry.SizeChanged += (s, e) =>
		//{
		//	tagsLabel.HeightRequest = descriptionEntry.Height;
		//};
	}

	private void saveExpense_Clicked(object sender, EventArgs e)
	{
		// Validate user input
		StringBuilder validationErrors = new StringBuilder();
		if (amountEntry.Value < 0)
			validationErrors.Append("Amount must not be negative.\n");
		if (model.IsRecurring)
		{
			if (!int.TryParse(frequencyEntry.Text, out int frequency))
				validationErrors.Append("Frequency must be an integer.\n");
			else if (frequency <= 0)
				validationErrors.Append("Frequency must be positive.\n");
		}
		if (validationErrors.Length != 0)
		{
			validationErrors.Length--; // remove newline
			DisplayAlert("Error", validationErrors.ToString(), "okay");
			return;

		}

		model.SaveClicked();
	}

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

	private void dateEntry_DateSelected(object sender, DateChangedEventArgs e) => model.DateChanged();
}