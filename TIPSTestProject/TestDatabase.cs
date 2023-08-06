using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using TIPS;

namespace TIPSTestProject
{
	[TestClass]
	public class TestDatabase : BasicAssert
	{
		private string testDbName = Path.Combine(Path.GetTempPath(), "test.db");
		private SQLiteService service;

		public TestDatabase()
		{
			service = new(testDbName);
		}

		[TestCleanup]
		public void Cleanup()
		{
			service.Close().Wait();
			File.Delete(testDbName);
		}

		[TestMethod]
		public async Task TestAddingTags()
		{
			Task t1 = service.AddTag("add1");
			Task t2 = service.AddTag("add2");
			await t1; await t2;

			HashSet<string> tags = (await service.GetAllTags()).ToHashSet();
			assert(tags.Contains("add1"));
			assert(tags.Contains("add2"));
		}

		[TestMethod]
		public async Task TestReaddingTag()
		{
			await service.AddTag("readd");
			
			IEnumerable<string> tags = await service.GetAllTags();
			assert(tags.Contains("readd"));
			int tagCount = tags.Count();

			await service.AddTag("readd");
			tags = await service.GetAllTags();
			assert(tags.Count() == tagCount);
		}

		[TestMethod]
		public async Task TestDeletingTags()
		{
			await service.AddTag("delete");

			HashSet<string> tags = (await service.GetAllTags()).ToHashSet();
			assert(tags.Contains("delete"));

			await service.DeleteTag("delete");
			tags = (await service.GetAllTags()).ToHashSet();
			assert(!tags.Contains("delete"));
		}
	}
}
