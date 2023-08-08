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

		public Expense EditedExpense { get; set; }
		private Expense? originalExpense { get; }

		public IEnumerable<string> AllTags { get => DefaultPlatformService.Instance.GetSQLiteService().GetAllTags().Result; }

		// Other things
		public PageResult Result { get; private set; }

		private PlatformServices platformServices;
		private ExpenseEditorUI ui;


		public ExpenseEditorModel(Expense? expenseToEdit, ExpenseEditorUI ui, PlatformServices? platformServices = null)
		{
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			this.ui = ui;
			originalExpense = expenseToEdit;

			if (originalExpense == null)
			{
				EditedExpense = new Expense(DateOnly.FromDateTime(DateTime.Now))
				{
					Amount = 1.23m, // TODO: Fix the UI? The amountEntry is setting itself to $0.00
					Description = "default",
				};
				IsNew = true;
				ui.HideDeleteButton();
			}
			else
			{
				EditedExpense = new Expense(originalExpense.Date);
				EditedExpense.CopyFrom(originalExpense);
				IsNew = false;
			}			
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
