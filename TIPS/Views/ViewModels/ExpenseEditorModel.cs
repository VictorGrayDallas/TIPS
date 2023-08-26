using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	internal class ExpenseEditorModel : INotifyPropertyChanged
	{
		// Required UI stuff
		public interface ExpenseEditorUI
		{
			void HideDeleteButton();

			void Close();

			List<string> GetTags();
		}

		// For data binding support
		public event PropertyChangedEventHandler? PropertyChanged;
		// Bindable properties
		private bool isNew;
		public bool IsNew {
			get => isNew;
			set
			{
				isNew = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNew)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotNew)));
			}
		}
		public bool NotNew { get => !IsNew; }

		public IEnumerable<string> AllTags { get => platformServices.GetSQLiteService().GetAllTags().Result; }

		public Expense EditedExpense { get; set; }

		public bool IsRecurring { get => EditedExpense is RecurringExpense; }
		public RecurringExpense? ExpenseAsRecurring { get => EditedExpense as RecurringExpense; }

		private bool _wasRecurringTrigger = false;
		public bool RecurringWillTriggerOnSave
		{
			get => IsRecurring && EditedExpense.Date <= DateOnly.FromDateTime(DateTime.Today);
		}
		public void DateChanged()
		{
			if (RecurringWillTriggerOnSave != _wasRecurringTrigger)
			{
				_wasRecurringTrigger = RecurringWillTriggerOnSave;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RecurringWillTriggerOnSave)));
			}
		}


		private Expense? originalExpense { get; }


		// Other things
		public PageResult Result { get; private set; }

		private PlatformServices platformServices;
		private ExpenseEditorUI ui;


		public ExpenseEditorModel(bool newExpenseIsRecurring, ExpenseEditorUI ui, PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			this.ui = ui;

			originalExpense = null;
			if (newExpenseIsRecurring)
				EditedExpense = new RecurringExpense(DateOnly.FromDateTime(DateTime.Now), 1, RecurringExpense.FrequencyUnits.Months);
			else
				EditedExpense = new Expense(DateOnly.FromDateTime(DateTime.Now));
			EditedExpense.Amount = 1.23m;
			EditedExpense.Description = "";

			IsNew = true;
			ui.HideDeleteButton();
		}
		public ExpenseEditorModel(Expense expenseToEdit, ExpenseEditorUI ui, PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			this.ui = ui;

			originalExpense = expenseToEdit;
			if (originalExpense is RecurringExpense re)
				EditedExpense = RecurringExpense.Copy(re);
			else
				EditedExpense = Expense.Copy(originalExpense);

			IsNew = false;
		}

		public void SaveClicked()
		{
			Result = PageResult.SAVE;
			EditedExpense.Tags = ui.GetTags();
			if (originalExpense != null)
			{
				// We need to edit and return the original object so as not to invalidate other references.
				originalExpense.CopyFrom(EditedExpense);
				EditedExpense = originalExpense;
			}

			ui.Close();
		}

		public void CancelClicked()
		{
			Result = PageResult.CANCEL;
			ui.Close();
		}

		public void DeleteClicked()
		{
			Result = PageResult.DELETE;
			if (originalExpense != null)
			{
				// We need to return the original object so the caller can delete it from the database.
				EditedExpense = originalExpense;
			}

			ui.Close();
		}
	}
}
