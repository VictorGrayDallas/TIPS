using SQLite;
using System.Collections.Generic;
using TIPS;

namespace TIPS.SQLite
{
	[Table("Expense")]
	internal class SQLiteExpense : Expense
	{
		// Properties we need to add for SQLite objects
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; } = -1;


		// Properties that SQLite should replace
		[Ignore]
		public new int Tags { get; set; }
		public List<int> TagIDs { get; set; } = new();

	}
}
