using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIPS.Models.SQLiteWrappers;
using TIPS.SQLite;

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
		public new byte[] Tags
		{
			get => sqlBase.Tags;
			set => sqlBase.Tags = value;
		}
		[Indexed]
		public new DateTime Date
		{
			get => sqlBase.Date;
			set
			{
				sqlBase.Date = value;
				base.Date = (sqlBase as Expense).Date;
			}
		}
		void ISQLiteExpense.ReceiveData(object data)
		{
			((ISQLiteExpense)sqlBase).ReceiveData(data);
			if (data is Dictionary<int, string> idDict)
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
