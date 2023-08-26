using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.ViewModels
{
	class ReportEditorModel : INotifyPropertyChanged
	{
		// Required UI stuff
		public interface ReportEditorUI
		{
			void HideDeleteButton();

			void Close();

			ReportColumn? GetSelectedColumn();
			List<string>? GetSelectedTagGroup();
		}

		// For data binding support
		public event PropertyChangedEventHandler? PropertyChanged;
		// Bindable properties
		private bool isNew;
		public bool IsNew
		{
			get => isNew;
			set
			{
				isNew = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNew)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NotNew)));
			}
		}
		public bool NotNew { get => !IsNew; }

		public ReportSettings EditedSettings { get; private set; }
		//public ReportColumn? SelectedColumn => ui.GetSelectedColumn();
		//public List<string>? SelectedTagGroup => ui.GetSelectedTagGroup();

		// Other things
		public PageResult Result { get; private set; }

		private ReportEditorUI ui;
		private PlatformServices platformServices;
		private ReportSettings? originalSettings;

		public IEnumerable<string> AllTags { get => platformServices.GetSQLiteService().GetAllTags().Result; }

		public ReportEditorModel(ReportSettings? settings, ReportEditorUI ui, PlatformServices? platformServices = null)
		{
			this.ui = ui;
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			originalSettings = settings;
			IsNew = settings != null;
			EditedSettings = settings?.Clone() ?? new ReportSettings();

			ui.HideDeleteButton();
		}

		public void SaveClicked()
		{
			Result = PageResult.SAVE;
			if (originalSettings != null)
			{
				// We need to edit and return the original object so as not to invalidate other references.
				originalSettings.CopyFrom(EditedSettings);
				EditedSettings = originalSettings;
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
			if (originalSettings != null)
			{
				// We need to return the original object so the caller can delete it from the database.
				EditedSettings = originalSettings;
			}

			ui.Close();
		}
	}
}
