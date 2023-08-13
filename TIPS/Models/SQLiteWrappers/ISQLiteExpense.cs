using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIPS.Models.SQLiteWrappers
{
	internal interface ISQLiteExpense
	{
		int Id { get; }

		void ReceiveData(object data);
	}
}
