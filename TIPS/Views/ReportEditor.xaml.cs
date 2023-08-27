using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class ReportEditor : ContentPage, ReportEditorModel.ReportEditorUI
{
	private ReportEditorModel model;

	internal event Action<ReportEditorModel>? Closing;

	private ReportColumn newColumn;
	private List<string> newRow;

	private bool suppressChanges = false;

	internal ReportEditor(ReportSettings settings)
	{
		InitializeComponent();
		unitPicker.ItemsSource = new RecurringExpense.FrequencyUnits[]
		{
				RecurringExpense.FrequencyUnits.Days,
				RecurringExpense.FrequencyUnits.Weeks,
				RecurringExpense.FrequencyUnits.Months,
				RecurringExpense.FrequencyUnits.Years,
		};

		model = new(settings, this);
		// No binding for the overall page here.

		titleEntry.Text = settings.Title;

		ObservableCollection<ReportColumn> columnsSource = new(model.EditedSettings.Columns);
		newColumn = new ReportColumn() { Header = "New..." };
		columnsSource.Add(newColumn);
		columnsView.ItemsSource = columnsSource;
		if (model.EditedSettings.Columns.Any())
			columnsView.SelectedItem = model.EditedSettings.Columns[0];

		ObservableCollection<List<string>>  rowsSource = new(model.EditedSettings.TagGroups);
		newRow = new List<string>() { "New..." };
		rowsSource.Add(newRow);
		tagGroupsView.ItemsSource = rowsSource;
		if (model.EditedSettings.TagGroups.Any())
			tagGroupsView.SelectedItem = model.EditedSettings.TagGroups[0];

		noTagsLabel.IsVisible = !model.EditedSettings.TagGroups.Any();

		tagPicker.ItemsSource = model.AllTags;
		tagPicker.AllowNewTags = false;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		Closing?.Invoke(model);
	}

	private void saveButton_Clicked(object sender, EventArgs e)
	{
		RemoveEmptyTagGroups();

		// User may have re-ordered columns and rows.
		ObservableCollection<ReportColumn> columnsSource = (ObservableCollection<ReportColumn>)columnsView.ItemsSource;
		model.EditedSettings.Columns.Clear();
		foreach (ReportColumn col in columnsSource)
		{
			if (col != newColumn)
				model.EditedSettings.Columns.Add(col);
		}

		ObservableCollection<List<string>> rowsSource = (ObservableCollection<List<string>>)tagGroupsView.ItemsSource;
		model.EditedSettings.TagGroups.Clear();
		foreach (List<string> row in rowsSource)
		{
			if (row != newRow)
				model.EditedSettings.TagGroups.Add(row);
		}

		model.SaveClicked();
	}

	private void cancelButton_Clicked(object sender, EventArgs e) => model.CancelClicked();

	private void deleteButton_Clicked(object sender, EventArgs e) => model.DeleteClicked();

	public void HideDeleteButton()
	{
		saveCancelGrid.ColumnDefinitions.RemoveAt(2);
		deleteButton.IsVisible = false;
	}

	public void Close()
	{
		Closing?.Invoke(model);
		Navigation.PopModalAsync();
	}

	public ReportColumn? GetSelectedColumn() => columnsView.SelectedItem as ReportColumn;

	public List<string>? GetSelectedTagGroup() => tagGroupsView.SelectedItem as List<string>;


	private string GetDefaultHeader(ReportColumn column)
	{
		string unitName = column.BaseUnit.ToString();
		if (column.IsRolling)
		{
			if (column.NumForAverage == 1)
				return $"Rolling total, 1 {unitName.ToLower()[..(unitName.Length - 1)]}";
			else
				return $"Average over {column.NumForAverage} {unitName.ToLower()}";
		}
		else
			return $"{unitName[..(unitName.Length - 1)]} to date";
	}


	private void headerEntry_TextChanged(object sender, TextChangedEventArgs e)
	{
		GetSelectedColumn()!.Header = headerEntry.Text;
		// Update list view
		ObservableCollection<ReportColumn> columnsSource = (ObservableCollection<ReportColumn>)columnsView.ItemsSource;
		int index = columnsSource.IndexOf(GetSelectedColumn()!);
		columnsSource[index] = columnsSource[index];
	}

	private void titleEntry_TextChanged(object sender, TextChangedEventArgs e) => model.EditedSettings.Title = titleEntry.Text ?? "[no title]";

	private void countEntry_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (suppressChanges)
			return;

		ReportColumn col = GetSelectedColumn()!;
		if (int.TryParse(countEntry.Text, out int count))
		{
			if (count > 0)
				col.NumForAverage = count;
			else
				countEntry.Text = "1";
		}
		else if (countEntry.Text == "")
			col.NumForAverage = 1;
		else
			countEntry.Text = col.NumForAverage.ToString();
	}

	private void typePicker_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (suppressChanges)
			return;

		ReportColumn col = GetSelectedColumn()!;
		bool isDefaultHeader = col.Header == GetDefaultHeader(col);

		col.IsRolling = typePicker.SelectedIndex != 0;
		if (typePicker.SelectedIndex == 1)
		{
			col.NumForAverage = 1;
		}
		else
		{
			col.NumForAverage = 3;
			countEntry.Text = "3";
		}
		countLabel.IsVisible = countEntry.IsVisible = typePicker.SelectedIndex == 2;

		if (isDefaultHeader)
		{
			col.Header = GetDefaultHeader(col);
			headerEntry.Text = col.Header;
		}
	}

	private void unitPicker_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (suppressChanges)
			return;

		ReportColumn col = GetSelectedColumn()!;
		bool isDefaultHeader = col.Header == GetDefaultHeader(col);

		if (unitPicker.SelectedIndex != -1)
			col.BaseUnit = (RecurringExpense.FrequencyUnits)unitPicker.SelectedIndex;
		if (isDefaultHeader)
		{
			col.Header = GetDefaultHeader(col);
			headerEntry.Text = col.Header;
		}
	}

	private void columnsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ReportColumn? col = GetSelectedColumn();
		if (col == newColumn)
		{
			var source = (columnsView.ItemsSource as ObservableCollection<ReportColumn>)!;
			col = new ReportColumn();
			col.Header = GetDefaultHeader(col);
			source.Insert(source.Count - 1, col);
			columnsView.SelectedItem = col;
			model.EditedSettings.Columns.Add(col);
		}

		columnGrid.IsVisible = col != null;
		if (col != null)
		{
			suppressChanges = true;
			headerEntry.Text = col.Header;
			if (col.IsRolling)
				typePicker.SelectedIndex = col.NumForAverage > 1 ? 2 : 1;
			else
				typePicker.SelectedIndex = 0;
			unitPicker.SelectedIndex = (int)col.BaseUnit;
			countEntry.Text = col.NumForAverage.ToString();
			suppressChanges = false;
		}
	}

	private void deleteColumn_Clicked(object sender, EventArgs e)
	{
		ReportColumn? col = GetSelectedColumn();
		if (col != null)
		{
			columnsView.SelectedItem = null;
			var source = (columnsView.ItemsSource as ObservableCollection<ReportColumn>)!;
			source.Remove(col);
			model.EditedSettings.Columns.Remove(col);
		}
	}

	private void RemoveEmptyTagGroups()
	{
		var source = (tagGroupsView.ItemsSource as ObservableCollection<List<string>>)!;
		var rows = model.EditedSettings.TagGroups;
		for (int i = 0; i < rows.Count; i++)
		{
			if (rows[i].Count == 0)
			{
				rows.RemoveAt(i);
				source.RemoveAt(i);
				i--;
			}
		}
	}

	private bool settingTags = false;
	private void tagGroupsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		List<string>? tags = GetSelectedTagGroup();
		// This event handler won't run while it's also running. So settingTags will always be false, we can't use that.
		if (tags == null || tags.Count != 0)
			RemoveEmptyTagGroups();

		settingTags = true;
		if (tags == newRow)
		{
			var source = (tagGroupsView.ItemsSource as ObservableCollection<List<string>>)!;
			tags = new List<string>();
			source.Insert(source.Count - 1, tags);
			tagGroupsView.SelectedItem = tags;
			model.EditedSettings.TagGroups.Add(tags);
			noTagsLabel.IsVisible = false;
		}

		tagStack.IsVisible = tags != null;
		if (tags != null)
			tagPicker.SetTags(tags);
		settingTags = false;
	}

	private void UpdateSelectedTagGroupDisplay()
	{
		List<string> tags = GetSelectedTagGroup()!;
		var source = (tagGroupsView.ItemsSource as ObservableCollection<List<string>>)!;
		int index = source.IndexOf(tags);
		source[index] = tags;
	}
	private void tagPicker_TagAdded(object sender, string tag)
	{
		if (settingTags)
			return;

		List<string> tags = GetSelectedTagGroup()!;
		tags.Add(tag);
		UpdateSelectedTagGroupDisplay();
	}
	private void tagPicker_TagRemoved(object sender, string tag)
	{
		if (settingTags)
			return;

		List<string> tags = GetSelectedTagGroup()!;
		tags.Remove(tag);
		UpdateSelectedTagGroupDisplay();
	}

	private void deleteRow_Clicked(object sender, EventArgs e)
	{
		List<string>? tags = GetSelectedTagGroup();
		if (tags != null)
		{
			tagGroupsView.SelectedItem = null;
			var source = (tagGroupsView.ItemsSource as ObservableCollection<List<string>>)!;
			source.Remove(tags);
			model.EditedSettings.TagGroups.Remove(tags);

			noTagsLabel.IsVisible = !source.Any();
		}
	}
}