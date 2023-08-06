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
		public new byte[] Tags {
			get
			{
				byte[] bytes = new byte[base.Tags.Count * sizeof(int)];
				for (int i = 0; i < base.Tags.Count; i++)
				{
					int id = SQLiteService._TagId(base.Tags[i]);
					BitConverter.TryWriteBytes(new Span<byte>(bytes, i * sizeof(int), sizeof(int)), id);
				}
				return bytes;
			}
			set
			{
				base.Tags = new List<string>();
				for (int i = 0; i < value.Length; i += sizeof(int))
				{
					int id = BitConverter.ToInt32(value, i);
					base.Tags.Add(SQLiteService._TagName(id));
				}
			}
		}

		// SQLite does not understand DateOnly.
		public new DateTime Date {
			get => base.Date.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
			set => base.Date = DateOnly.FromDateTime(value);
		}

		public SQLiteExpense(Expense expense) : base(expense.Date)
		{
			Description = expense.Description;
			Amount = expense.Amount;
			base.Tags = expense.Tags;
		}
		/// <summary>
		/// Required by SQLite. Do not use.
		/// </summary>
		public SQLiteExpense() : base(new DateOnly()) { }
	}
}
