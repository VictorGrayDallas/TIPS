using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views;

public partial class ReportView : VerticalStackLayout, ReportViewModel.ReportViewUI
{
	private ReportViewModel model;

	public event Action<ReportView>? EditClicked;

	private static Color backgroundColorDarkMode = Colors.Black;
	private static Color backgroundColorLightMode = Colors.White;
	private static Color foregroundColorDarkMode = Colors.White;
	private static Color foregroundColorLightMode = Colors.Black;

	private Grid grid = null!;
	private Label titleLabel = new();
	private List<Label> columnHeaderLabels = new();
	private List<Label> rowHeaderLabels = new();
	private List<List<Label>> dataLabels = new();

	internal ReportView(ReportSettings settings)
	{
		InitializeComponent();

		model = new(settings, this);
	}

	public void UpdateSettings(ReportSettings settings)
	{
		titleLabel.Text = settings.Title;
		model.UpdateSettings(settings);
	}

	public async Task RefreshData(bool recalculate = false)
	{
		if (recalculate)
			await model.RefreshData(); // will re-call this.RefershData
		else
		{
			for (int i = 0; i < rowHeaderLabels.Count; i++)
				rowHeaderLabels[i].Text = string.Join(", ", model.EffectiveTagGroups[i]);
			for (int i = 0; i < dataLabels.Count; i++)
			{
				for (int j = 0; j < dataLabels[i].Count; j++)
					dataLabels[i][j].Text = model.GetValue(i, j).ToString("C");
			}
		}
	}

	public void RebuildGrid()
	{
		CreateControls();
	}

	public ReportSettings GetSettings() => model.GetSettings();

	private BoxView MakeCellBackground()
	{
		BoxView box = new BoxView()
		{
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill,
		};
		box.SetAppThemeColor(BoxView.ColorProperty, backgroundColorLightMode, backgroundColorDarkMode);
		return box;
	}
	private Label MakeLabel()
	{
		Label label = new Label();
		label.SetAppThemeColor(Label.BackgroundColorProperty, backgroundColorLightMode, backgroundColorDarkMode);
		label.SetAppThemeColor(Label.TextColorProperty, foregroundColorLightMode, foregroundColorDarkMode);
		return label;
	}
	private Label MakeCellLabel()
	{
		double cellWidthPadding = 4;
		double cellHeightPadding = 3;

		Label label = MakeLabel();
		label.VerticalOptions = LayoutOptions.Center;
		label.Padding = new Thickness(cellWidthPadding, cellHeightPadding);
		return label;
	}
	private Label MakeHeaderLabel()
	{
		LayoutOptions columnHeaderHorizontalOptions = LayoutOptions.Center;

		Label label = MakeCellLabel();
		label.HorizontalOptions = columnHeaderHorizontalOptions;
		label.FontAttributes = FontAttributes.Bold;

		return label;
	}
	private void CreateControls()
	{
		this.Clear();

		ReportSettings settings = model.GetSettings();
		int columns = 1 + settings.Columns.Count;

		// const values
		string Pencil = "\xf2bf";
		double gridLineThickness = 1;

		Grid topLayout = new Grid();
		topLayout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		topLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		topLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		topLayout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

		// Title
		titleLabel = MakeLabel();
		titleLabel.FontSize = 24;
		titleLabel.VerticalOptions = LayoutOptions.Center;
		topLayout.Add(titleLabel);

		// Edit button
		ImageButton editButton = new();
		editButton.SetAppThemeColor(ImageButton.BackgroundColorProperty, backgroundColorLightMode, backgroundColorDarkMode);
		editButton.Source = new FontImageSource()
		{
			Glyph = Pencil,
			FontFamily = "ionicons",
		};
		editButton.Source.SetAppThemeColor(FontImageSource.ColorProperty, foregroundColorLightMode, foregroundColorDarkMode);
		editButton.Clicked += (s, e) => EditClicked?.Invoke(this);
		topLayout.Add(editButton, 1);

		// Grid
		grid = new Grid()
		{
			RowSpacing = gridLineThickness,
			ColumnSpacing = gridLineThickness,
		};
		grid.SetAppThemeColor(Grid.BackgroundColorProperty, foregroundColorLightMode, foregroundColorDarkMode);
		grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
		for (int i = 1; i < columns; i++)
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

		// Column headers
		columnHeaderLabels.Clear();
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
			grid.Add(MakeCellBackground(), i, 0);

		Label chl = MakeHeaderLabel();
		columnHeaderLabels.Add(chl);
		grid.Add(chl, 0, 0);
		for (int i = 0; i < settings.Columns.Count; i++)
		{
			chl = MakeHeaderLabel();
			columnHeaderLabels.Add(chl);
			grid.Add(chl, i + 1, 0);
		}

		// Rows
		dataLabels.Clear();
		rowHeaderLabels.Clear();
		for (int i = 0; i < model.GetRowCount(); i++)
			AddRow();

		UpdateNonDataLabels();

		this.Spacing = 8;
		this.Add(topLayout); // FYI: If you break on all exceptions, as is sometimes necessary to debug MAUI, this will break for not finding the ionicons font. That's OK, MAUI catches it and loads the font.
		this.Add(grid);
	}
	private void AddRow()
	{
		grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		int row = grid.RowDefinitions.Count - 1;

		for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
			grid.Add(MakeCellBackground(), i, row);

		// row header
		Label label = MakeCellLabel();
		rowHeaderLabels.Add(label);
		grid.Add(label, 0, row);
		// data cells
		ReportSettings settings = model.GetSettings();
		dataLabels.Add(new List<Label>());
		for (int i = 0; i < settings.Columns.Count; i++)
		{
			label = MakeCellLabel();
			dataLabels[row - 1].Add(label);
			grid.Add(label, i + 1, row);
		}
	}

	public void UpdateNonDataLabels()
	{
		ReportSettings settings = model.GetSettings();
		titleLabel.Text = settings.Title;
		columnHeaderLabels[0].Text = "Tag(s)";
		for (int i = 1; i < columnHeaderLabels.Count; i++)
			columnHeaderLabels[i].Text = settings.Columns[i - 1].Header;
	}
}