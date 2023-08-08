using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace TIPS.Views
{
	internal class MoneyEntry : Entry
	{

		public static BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(decimal), typeof(MoneyEntry), 0m, 
			propertyChanged: (bindable, oldVal, newVal) =>
			{
				MoneyEntry moneyEntry = (MoneyEntry)bindable;
				moneyEntry.Text = ((decimal)newVal).ToString("C");
			});
		public decimal Value
		{
			get { return (decimal)GetValue(ValueProperty); }
			set
			{
				SetValue(ValueProperty, value);
				if (Value.ToString("C") != Text && Value.ToString("C") == aboutToSet)
				{
					aboutToSet = value.ToString("C");
					// I do not know if these are appropriate values for desiredCursorPosition and expectedAutomaticAdjustment.
					// It is possible that the appropriate values will depend on when or how the Value property is set.
					// At present, I have no need to set Value from outside OnTextChanged, so I have no use case to test.
					desiredCursorPosition = aboutToSet.Length;
					expectedAutomaticAdjustment = 0;
					Text = aboutToSet;
				}
			}
		}

		public MoneyEntry()
		{
			Keyboard = Keyboard.Numeric;
			aboutToSet = 0m.ToString("C");
			Text = aboutToSet;
			Value = 0m;
		}

		// The way the Entry moves the cursor position, as well as WHEN it moves it, is exteremly bizarre.
		private string aboutToSet = "";
		private int desiredCursorPosition = 0;
		private int expectedAutomaticAdjustment = 0;
		protected override void OnTextChanged(string oldText, string newText)
		{
			base.OnTextChanged(oldText, newText);
			// The way the Entry moves the cursor position, as well as WHEN it moves it, is exteremly bizarre.
			if (newText == aboutToSet)
			{
				CursorPosition = desiredCursorPosition - expectedAutomaticAdjustment;
				return;
			}

			if (!decimal.TryParse(newText.Replace("$", ""), out decimal value))
			{
				if (string.IsNullOrWhiteSpace(newText))
				{
					value = 0m;
					Text = value.ToString("C");
				}
				else
					Text = oldText;
				return;
			}

			// This code runs before CursorPosition gets updated by the user's typing
			bool userTypedAtEnd = CursorPosition == oldText.Length;
			bool userPasted = false;
			if (userTypedAtEnd)
			{
				if (Text.StartsWith(oldText) && Text.Length == oldText.Length + 1)
				{
					// The user placed a new number at the end; it will be afte the decimal point but we should move the decimal point.
					value *= 10;
				}
				else if (Text.Length == oldText.Length - 1)
				{
					// The user backspaced at the end; move decimal poitn other way
					value /= 10;
				}
				else if (Text.Length > oldText.Length)
				{
					// Probably a paste
					userPasted = true;
					if (Text.StartsWith(oldText)) // user did not highlight and replace old text
					{
						string pastedString = Text.Substring(Text.IndexOf(CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalSeparator) + 3);
						if (decimal.TryParse(pastedString, out value))
						{
							// user can't paste decimal point (unless they replaced old text) because Numeric keyboard forbids having two
							value /= 100;
						}
						else
							value = 0m;
					}
				}
			}
			// else, if user did not type at the end, we'll not try to change anything

			// The way the Entry moves the cursor position, as well as WHEN it moves it, is exteremly bizarre.
			// I would write comments explaining how this all works, but... I don't really quite know how this all works.
			aboutToSet = value.ToString("C");
			if (userTypedAtEnd || userPasted)
			{
				desiredCursorPosition = aboutToSet.Length;
				expectedAutomaticAdjustment = 0;
			}
			else
			{
				int firstGuess = CursorPosition + (newText.Length - oldText.Length);
				if (firstGuess < 0)
					firstGuess = 0;
				if (newText[..firstGuess] == aboutToSet[..firstGuess])
				{
					desiredCursorPosition = firstGuess;
					expectedAutomaticAdjustment = newText.Length - oldText.Length;
					// Edge case: backspacing a comma
					if (aboutToSet == oldText)
					{
						desiredCursorPosition = CursorPosition - 1;
						expectedAutomaticAdjustment = 0;
					}
				}
				else
				{
					desiredCursorPosition = firstGuess + (aboutToSet.Length - newText.Length);
					expectedAutomaticAdjustment = 0;
				}
			}
			Text = aboutToSet;
			Value = value;
		}
	}
}
