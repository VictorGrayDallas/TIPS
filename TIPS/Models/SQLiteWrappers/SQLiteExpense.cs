using SQLite;
using System;
using System.Collections.Generic;
using TIPS;
using TIPS.Models.SQLiteWrappers;

namespace TIPS.SQLite
{
	[Table("Expense")]
	class SQLiteExpense : Expense, ISQLiteExpense
	{
		// Properties we need to add for SQLite objects
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; } = -1;

		// Properties that SQLite should replace
		private byte[]? tagBlobGet;
		private byte[]? tagBlobSet;
		public new byte[] Tags {
			get
			{
				if (tagBlobGet == null)
					throw new SQLiteServiceException("Attempted to retrieve tag IDs from an expense without first calling ReceiveData.");
				if (tagBlobSet != null)
					throw new SQLiteServiceException("Attempted to retrieve tag IDs from an expense before assigning tag names.");
				byte[] temp = tagBlobGet;
				tagBlobGet = null;
				return temp;
			}
			set => tagBlobSet = value;
		}

		// SQLite does not understand DateOnly.
		// Plus we want to index it
		[Indexed]
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

		void ISQLiteExpense.ReceiveData(object data)
		{
			if (tagBlobGet != null)
				throw new SQLiteServiceException("Repeat call to expense ReceiveData.");

			if (data is Dictionary<string, int> tagDict)
			{
				byte[] bytes = new byte[base.Tags.Count * sizeof(int)];
				for (int i = 0; i < base.Tags.Count; i++)
				{
					int id = tagDict[base.Tags[i]];
					BitConverter.TryWriteBytes(new Span<byte>(bytes, i * sizeof(int), sizeof(int)), id);
				}
				tagBlobGet = bytes;
			}
			else if (data is Dictionary<int, string> idDict)
			{
				if (tagBlobSet == null)
					throw new SQLiteServiceException("Attempted to load tag names on expense without first setting IDs.");

				base.Tags = new List<string>();
				for (int i = 0; i < tagBlobSet.Length; i += sizeof(int))
				{
					int id = BitConverter.ToInt32(tagBlobSet, i);
					base.Tags.Add(idDict[id]);
				}
				tagBlobSet = null;
			}

			else
				throw new SQLiteServiceException("SQLiteExpense received invalid data.");
		}
	}
}
