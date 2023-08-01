using SQLite;

namespace TIPS.SQLite
{

	[Table("Tag")]

	internal class SQLiteTag
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; } = -1;


		public string TagName { get; set; } = "[unnamed]";
	}
}
