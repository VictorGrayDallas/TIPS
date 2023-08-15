using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TIPS.Models.SQLiteWrappers;

namespace TIPS.SQLite
{
	[Table("RecurringExpense")]
	class SQLiteRecurringExpense : RecurringExpense, ISQLiteExpense
	{
		// Properties we need to add for SQLite objects
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; } = -1;

		// "Inherit" from sqlBase
		private SQLiteExpense sqlBase;

		[Ignore]
		[Obsolete(SQLiteService.ErrorMessageForIgnoredFields, true)]
		public new List<string> Tags { get => base.Tags; set => base.Tags = value; }
		public byte[] Sql_Tags
		{
			get => sqlBase.Sql_Tags;
			set => sqlBase.Sql_Tags = value;
		}

		[Ignore]
		[Obsolete(SQLiteService.ErrorMessageForIgnoredFields, true)]
		public new DateOnly Date { get => base.Date; set => base.Date = value; }
		[Indexed]
		public DateTime Sql_Date
		{
			get
			{
				(sqlBase as Expense).Date = base.Date;
				return sqlBase.Sql_Date;
			}
			set
			{
				sqlBase.Sql_Date = value;
				base.Date = (sqlBase as Expense).Date;
			}
		}

		void ISQLiteExpense.ReceiveData(object data)
		{
			if (data is Dictionary<string, int>)
				(sqlBase as Expense).Tags = base.Tags;
			((ISQLiteExpense)sqlBase).ReceiveData(data);
			if (data is Dictionary<int, string>)
				base.Tags = (sqlBase as Expense).Tags;
		}


		public SQLiteRecurringExpense(RecurringExpense expense) : base(expense.Date, expense.Frequency, expense.FrequencyUnit)
		{
			// We need to copy non-replaced and base properties
			Amount = expense.Amount;
			Description = expense.Description;
			base.Tags = expense.Tags;
			// sqlBse holds replaced properties from SQLiteExpense (must be done after copying base properties)
			sqlBase = new SQLiteExpense(this);
		}
		/// <summary>
		/// Required by SQLite. Do not use.
		/// </summary>
		public SQLiteRecurringExpense() : base(new DateOnly(), 0, FrequencyUnits.Days)
		{
			sqlBase = new SQLiteExpense(this);
		}

	}
}
