using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TIPS.Views;

namespace TIPS.ViewModels
{
	internal class ReportViewModel
	{
		public interface ReportViewUI
		{
			public void RebuildGrid();

			public Task RefreshData(bool recalculate = false);

			public void UpdateNonDataLabels();
		}

		private ReportSettings settings;
		public ReportSettings GetSettings() => settings;

		ReportViewUI ui;
		PlatformServices platformServices;

		public List<List<string>> EffectiveTagGroups { get; set; } = new();

		private List<List<decimal>> data = new();
		

		public ReportViewModel(ReportSettings settings, ReportViewUI ui, PlatformServices? platformServices = null)
		{
			this.ui = ui;
			this.platformServices = platformServices ?? DefaultPlatformService.Instance;
			this.settings = settings;

			_ = RefreshData();
		}
		public async Task RefreshData()
		{
			// This is messy, but I don't know a good way to ensure certain bits of code run on the UI thread.
			// MainThread.BeginInvokeOnMainThread would work except it is MAUI-exclusive and so not testable.
			bool rebuildGrid = await Task.Run(async () =>
			{
				SQLiteService sqlService = platformServices.GetSQLiteService();
				DateOnly earliestColumnPeriod = settings.Columns.Min((c) => c.BeginningOfPeriod);
				IEnumerable<Expense> expenses = await sqlService.GetExpenses(earliestColumnPeriod, DateOnly.FromDateTime(DateTime.Today)).ConfigureAwait(false);

				List<List<string>> tagLists = settings.TagGroups;
				bool rebuildGrid = false;
				if (!tagLists.Any())
				{
					// Automatically put each tag in it's own group if no tag groups were specified.
					tagLists = new();
					var allTags = await sqlService.GetAllTags().ConfigureAwait(false);
					foreach (string tag in allTags)
						tagLists.Add(new List<string>() { tag });
					tagLists.Sort((l1, l2) => l1[0].CompareTo(l2[0]));
					rebuildGrid = tagLists.Count != GetRowCount();
				}
				if (!rebuildGrid)
					rebuildGrid = tagLists.Count != GetRowCount() || !data.Any() || settings.Columns.Count != data[0].Count;
				EffectiveTagGroups = tagLists;

				data = new();
				DateOnly today = DateOnly.FromDateTime(DateTime.Today);
				foreach (List<string> tags in tagLists)
				{
					List<decimal> newDataRow = new();
					data.Add(newDataRow);
					int row = data.Count - 1;

					List<Expense> withTag = expenses.Where((e) => ListContainsAtLeastOne(e.Tags, tags)).ToList();
					for (int i = 0; i < settings.Columns.Count; i++)
					{
						ReportColumn col = settings.Columns[i];
						decimal value = withTag
							.Where((e) => e.Date >= col.BeginningOfPeriod && e.Date <= today)
							.Sum((e) => e.Amount);
						if (col.IsRolling)
							value /= col.NumForAverage;
						newDataRow.Add(value);
					}
				}

				return rebuildGrid;
			});

			if (rebuildGrid)
				ui.RebuildGrid();
			else
				ui.UpdateNonDataLabels();
			_ = ui.RefreshData();
		}

		private bool ListContainsAtLeastOne<T>(IEnumerable<T> first, IEnumerable<T> second)
		{
			foreach (T v in first)
				if (second.Contains(v)) return true;
			return false;
		}


		public void UpdateSettings(ReportSettings settings, bool refreshData = true)
		{
			this.settings = settings;
			if (refreshData)
				_ = RefreshData();
		}

		public int GetRowCount() => data.Count;

		public decimal GetValue(int row, int col) => data[row][col];
	}
}
