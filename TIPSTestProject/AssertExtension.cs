using System.Runtime.CompilerServices;

namespace TIPSTestProject
{
	public abstract class BasicAssert
	{
		// We don't need fancy assert methods. Just one will do in most cases.
		// Simple = good
		protected void assert(bool condition, string? failMessage = null)
		{
			if (failMessage != null)
				Assert.IsTrue(condition, failMessage);
			else
				Assert.IsTrue(condition);
		}
	}
}
