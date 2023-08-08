using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TIPS.ViewModels;

namespace TIPS.Views
{
	/// <summary>
	/// This is a modified custom control; the original was taken from Liyun Zhang's answer at https://stackoverflow.com/a/76118060
	/// </summary>
	internal class TagPicker : VerticalStackLayout
	{
		private Entry _entry;
		private ListView _listView;
		private FlexLayout _tagsLayout;
		private Dictionary<string, Button> _tagLabels = new();

		//Bindable properties
		#region "Bindable Properties"
		public static readonly BindableProperty ListViewHeightRequestProperty = BindableProperty.Create(nameof(ListViewHeightRequest), typeof(double), typeof(TagPicker), defaultValue: null, propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._listView.HeightRequest = (double)newVal;
		});
		public double ListViewHeightRequest
		{
			get { return (double)GetValue(ListViewHeightRequestProperty); }
			set { SetValue(ListViewHeightRequestProperty, value); }
		}
		public static readonly BindableProperty EntryBackgroundColorProperty = BindableProperty.Create(nameof(EntryBackgroundColor), typeof(Color), typeof(TagPicker), defaultValue: null, propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._entry.BackgroundColor = (Color)newVal;
		});
		public Color EntryBackgroundColor
		{
			get { return (Color)GetValue(EntryBackgroundColorProperty); }
			set { SetValue(EntryBackgroundColorProperty, value); }
		}
		public static readonly BindableProperty EntryFontSizeProperty = BindableProperty.Create(nameof(EntryFontSize), typeof(double), typeof(TagPicker), defaultValue: null, propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._entry.FontSize = (double)newVal;
		});
		[TypeConverter(typeof(FontSizeConverter))]
		public double EntryFontSize
		{
			get { return (double)GetValue(EntryFontSizeProperty); }
			set { SetValue(EntryFontSizeProperty, value); }
		}

		private IEnumerable<string> _itemsSource = new string[] { };
		public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<string>), typeof(TagPicker), defaultValue: null, propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._itemsSource = (IEnumerable<string>)newVal;
			picker.FilterTags(picker._entry.Text);
		});
		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}
		public static new readonly BindableProperty VisualProperty = BindableProperty.Create(nameof(Visual), typeof(IVisual), typeof(TagPicker), defaultValue: new VisualMarker.DefaultVisual(), propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._listView.Visual = (IVisual)newVal;
			picker._entry.Visual = (IVisual)newVal;
		});
		public new IVisual Visual
		{
			get { return (IVisual)GetValue(VisualProperty); }
			set { SetValue(VisualProperty, value); }
		}
		public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TagPicker), defaultValue: "", propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._entry.Placeholder = (string)newVal;
		});
		public string Placeholder
		{
			get { return (string)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}
		public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TagPicker), defaultValue: "", propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._entry.Text = (string)newVal;
		});
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		#endregion

		private bool ShouldShowListView { get => _listView.ItemsSource != null && (_listView.ItemsSource as IEnumerable<string>)!.Any(); }

		public event EventHandler<string>? TagAdded;
		public event EventHandler<TextChangedEventArgs>? TextChanged;

		public List<string> Tags { get => _tagLabels.Keys.ToList(); }

		private void CreateControls()
		{
			_entry = new Entry();
			_listView = new ListView();
			_tagsLayout = new FlexLayout();

			//Entry used for filtering list view
			_entry.Margin = new Thickness(0);
			_entry.Keyboard = Keyboard.Create(KeyboardFlags.None);

			//List view - used to display search options
			_listView.IsVisible = false;
			_listView.Margin = new Thickness(0);
			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.ListView.SetSeparatorStyle(_listView, Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.SeparatorStyle.FullWidth);
			_listView.MaximumHeightRequest = 100;

			//Add bottom border
			var boxView = new BoxView();
			boxView.HeightRequest = 1;
			boxView.Color = Colors.Black;
			boxView.Margin = new Thickness(0);
			boxView.SetBinding(BoxView.IsVisibleProperty, new Binding(nameof(ListView.IsVisible), source: _listView));

			_tagsLayout.JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start;
			_tagsLayout.Direction = Microsoft.Maui.Layouts.FlexDirection.Row;
			_tagsLayout.Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap;

			Children.Add(_entry);
			Children.Add(_listView);
			Children.Add(boxView);
			Children.Add(_tagsLayout);

		}
		private void SetControlEventHandlers()
		{
			_listView.ItemSelected += (sender, args) =>
			{
				if (args.SelectedItem is string item && item != null)
				{
					_entry.Text = "";
					_entry.Unfocus();

					AddTag(item);
				}
				if (args.SelectedItem != null)
					_listView.SelectedItem = null;
			};

			//Text changed event, bring it back to the surface
			_entry.TextChanged += (sender, args) =>
			{
				FilterTags(_entry.Text);
				_listView.IsVisible = ShouldShowListView;
				TextChanged?.Invoke(this, args);
			};
			_entry.Completed += (sender, args) =>
			{
				if (_entry.Text != "")
				{
					AddTag(_entry.Text);
					_entry.Text = "";
				}
			};

			_entry.Focused += (sender, args) => _listView.IsVisible = ShouldShowListView;
			_entry.Unfocused += (sender, args) =>
			{
				_listView.IsVisible = false;
				if (!string.IsNullOrEmpty(_entry.Text))
				{
					AddTag(_entry.Text);
					_entry.Text = "";
				}
			};
		}
		public TagPicker()
		{
			// CreateControls sets these, but analyzer is dumb
			_entry = null!; _listView = null!; _tagsLayout = null!;

			CreateControls();
			SetControlEventHandlers();
		}

		public new bool Focus()
		{
			return _entry.Focus();
		}
		public new void Unfocus()
		{
			_entry.Unfocus();
		}

		private void FilterTags(string? filter)
		{
			filter = filter ?? "";
			_listView.ItemsSource = _itemsSource
				.Where((t) => t.ToLower().Contains(filter.ToLower()))
				.Where((t) => !_tagLabels.ContainsKey(t));
		}

		private void AddTag(string tagName)
		{
			Button label = new Button()
			{
				Text = tagName,
				Margin = new Thickness(6, 2),
				//BackgroundColor = Colors.Transparent,
			};

			label.Clicked += (s, e) => EditTag((s as Button)!.Text);

			_tagLabels[tagName] = label;
			_tagsLayout.Add(label);

			TagAdded?.Invoke(this, tagName);
		}
		private void EditTag(string tagName)
		{
			_entry.Text = tagName;
			_entry.Focus();

			_tagsLayout.Remove(_tagLabels[tagName]);
			_tagLabels.Remove(tagName);
		}
	}
}
