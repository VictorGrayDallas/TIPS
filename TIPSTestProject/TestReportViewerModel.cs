using Microsoft.VisualStudio.TestTools.UnitTesting;
using TIPS;
using TIPS.ViewModels;

namespace TIPSTestProject
{
	[TestClass]
	public class TestReportViewerModel : BasicAssert
	{
		internal class TestUI : ReportViewModel.ReportViewUI
		{
			public void RebuildGrid() { }

			public void RefreshData()
			{
				refreshed = true;
			}

			public void UpdateNonDataLabels() { }

			private bool refreshed = false;
			public async Task WaitForRefreshData()
			{
				int count = 0;
				while (!refreshed && count < 100)
				{
					await Task.Delay(50);
					count++;
				}
				Assert.IsTrue(count < 100, "WaitForRefreshData timed out.");

				refreshed = false;
			}
		}


		private static TestPlatformService service = null!; // set in class init method

		public TestReportViewerModel() { }

		// Note: The TestContext parameter is required by MSTest. It will silently fail if it is not present.
		[ClassInitialize]
		public static async Task CreateDatabase(TestContext testContext)
		{
			service = new TestPlatformService("Reports.db");

			Expense baseExpense = new Expense(DateOnly.FromDateTime(DateTime.Today))
			{
				Amount = 1m,
				Tags = new List<string>() { "test" },
			};

			SQLiteService sqlService = service.GetSQLiteService();
			await sqlService.AddSingleExpense(baseExpense);

			Expense other = Expense.Copy(baseExpense);
			other.Date = baseExpense.Date.AddMonths(-1);
			await sqlService.AddSingleExpense(other);

			other = Expense.Copy(baseExpense);
			other.Tags = new List<string>();
			await sqlService.AddSingleExpense(other);
		}

		[ClassCleanup]
		public static async Task Cleanup()
		{
			await service.GetSQLiteService().Close();
			File.Delete(Path.Combine(service.AppDataPath, service.DefaultDatabaseName));
		}

		[TestMethod]
		public async Task TestCurrentMonth()
		{
			ReportSettings reportSettings = new ReportSettings()
			{
				Title = "Report",
				Columns = new List<ReportColumn>()
				{
					new ReportColumn() {
						Header = "Month to date",
						IsRolling = false,
					},
				}
			};

			TestUI ui = new();
			ReportViewModel report = new ReportViewModel(reportSettings, ui, service);
			await ui.WaitForRefreshData();

			assert(report.GetValue(0, 0) == 1m);
		}

		[TestMethod]
		public async Task TestRollingMonth()
		{
			ReportSettings reportSettings = new ReportSettings()
			{
				Title = "Report",
				Columns = new List<ReportColumn>()
				{
					new ReportColumn() {
						Header = "Past month",
						IsRolling = true,
						NumForAverage = 1,
					},
				}
			};

			TestUI ui = new();
			ReportViewModel report = new ReportViewModel(reportSettings, ui, service);
			await ui.WaitForRefreshData();

			assert(report.GetValue(0, 0) == 1m);
		}

		[TestMethod]
		public async Task TestAverage()
		{
			ReportSettings reportSettings = new ReportSettings()
			{
				Title = "Report",
				Columns = new List<ReportColumn>()
				{
					new ReportColumn() {
						Header = "Average over 4 months",
						IsRolling = true,
						NumForAverage = 4,
					},
				}
			};

			TestUI ui = new();
			ReportViewModel report = new ReportViewModel(reportSettings, ui, service);
			await ui.WaitForRefreshData();

			assert(report.GetValue(0, 0) == 2m / 4);
		}

	}
}
