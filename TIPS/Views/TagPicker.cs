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
		private CollectionView _collectionView;
		private FlexLayout _tagsLayout;
		private Dictionary<string, Button> _tagLabels = new();

		//Bindable properties
		#region "Bindable Properties"
		public static readonly BindableProperty CollectionViewHeightRequestProperty = BindableProperty.Create(nameof(CollectionViewHeightRequest), typeof(double), typeof(TagPicker), defaultValue: null, propertyChanged: (bindable, oldVal, newVal) => {
			var picker = (TagPicker)bindable;
			picker._collectionView.HeightRequest = (double)newVal;
		});
		public double CollectionViewHeightRequest
		{
			get { return (double)GetValue(CollectionViewHeightRequestProperty); }
			set { SetValue(CollectionViewHeightRequestProperty, value); }
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
			picker._collectionView.Visual = (IVisual)newVal;
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

		private bool ShouldShowCollectionView { get => _collectionView.ItemsSource != null && (_collectionView.ItemsSource as IEnumerable<string>)!.Any(); }

		public event EventHandler<string>? TagAdded;
		public event EventHandler<string>? TagRemoved;
		public event EventHandler<TextChangedEventArgs>? TextChanged;

		public List<string> Tags { get => _tagLabels.Keys.ToList(); }
		public void SetTags(List<string> tags)
		{
			_tagsLayout.Clear();
			_tagLabels.Clear();
			Tags.Clear();

			foreach (string tag in tags)
				AddTag(tag);
		}

		public bool AllowNewTags = true;

		private void CreateControls()
		{
			_entry = new Entry();
			_collectionView = new CollectionView();
			_tagsLayout = new FlexLayout();

			//Entry used for filtering list view
			_entry.Margin = new Thickness(0);
			_entry.Keyboard = Keyboard.Create(KeyboardFlags.None);

			//List view - used to display search options
			_collectionView.IsVisible = false;
			_collectionView.SelectionMode = SelectionMode.Single;
			_collectionView.Margin = new Thickness(0);
			_collectionView.MaximumHeightRequest = 100;
			_collectionView.VerticalOptions = LayoutOptions.Start; // This is required, or scrolling will be very broken. With this, it's only sometimes a little broken.
			// We need to use padding instead of CollectionView.ItemsLayout.ItemsSpacing so that users can tap the padding space to select items.
			_collectionView.ItemTemplate = new DataTemplate(() =>
			{
				Label l = new Label();
				l.Padding = new Thickness(0, 5);
				l.SetBinding(Label.TextProperty, ".");
				return l;
			});

			//Add bottom border
			var boxView = new BoxView();
			boxView.HeightRequest = 1;
			boxView.Color = Colors.Black;
			boxView.Margin = new Thickness(0);
			boxView.SetBinding(BoxView.IsVisibleProperty, new Binding(nameof(CollectionView.IsVisible), source: _collectionView));

			_tagsLayout.JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start;
			_tagsLayout.Direction = Microsoft.Maui.Layouts.FlexDirection.Row;
			_tagsLayout.Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap;

			Children.Add(_entry);
			Children.Add(_collectionView);
			Children.Add(boxView);
			Children.Add(_tagsLayout);

		}
		private void SetControlEventHandlers()
		{
			_collectionView.SelectionChanged += (sender, args) =>
			{
				if (args.CurrentSelection.FirstOrDefault() is string item && item != null)
				{
					_entry.Text = "";
					_entry.Unfocus();

					AddTag(item);
				}
				if (args.CurrentSelection.FirstOrDefault() != null)
					_collectionView.SelectedItem = null;
			};

			//Text changed event, bring it back to the surface
			_entry.TextChanged += (sender, args) =>
			{
				FilterTags(_entry.Text);
				_collectionView.IsVisible = ShouldShowCollectionView;
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

			_entry.Focused += (sender, args) => _collectionView.IsVisible = ShouldShowCollectionView;
			_entry.Unfocused += (sender, args) =>
			{
				_collectionView.IsVisible = false;
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
			_entry = null!; _collectionView = null!; _tagsLayout = null!;

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
			_collectionView.ItemsSource = _itemsSource
				.Where((t) => t.ToLower().Contains(filter.ToLower()))
				.Where((t) => !_tagLabels.ContainsKey(t));
		}

		private void AddTag(string tagName)
		{
			if (!AllowNewTags && !((IEnumerable<string>)ItemsSource).Contains(tagName))
				return;

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

			TagRemoved?.Invoke(this, tagName);
		}
	}
}
