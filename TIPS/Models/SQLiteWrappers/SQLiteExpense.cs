using SQLite;
using System;
using System.Collections.Generic;
using TIPS;

namespace TIPS.SQLite
{
	[Table("Expense")]
	class SQLiteExpense : Expense
	{
		// Properties we need to add for SQLite objects
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; } = -1;


		// Properties that SQLite should replace
		[Ignore]
		public new int Tags { get; set; }
		public List<int> TagIDs { get; set; } = new();


		public SQLiteExpense(DateOnly date) : base(date) { }

		/// <summary>
		/// Required by SQLite. Do not use.
		/// </summary>
		public SQLiteExpense() : base(new DateOnly()) { }
	}
}
