using System;

namespace Alter.Migrations
{
	public class MigrationException : Exception
	{
		public MigrationException (string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}

