using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alter.Migrations
{
	public class ConsoleAdapter : DatabaseAdapter
	{
		public string InfoMessage {
			get {
				throw new NotImplementedException ();
			}
		}

		public System.Data.Common.DbCommand BuildCommand ()
		{
			throw new NotImplementedException ();
		}

		public System.Data.Common.DbConnection Connection {
			get {
				throw new NotImplementedException ();
			}
		}

		public string GetNativeBaseline ()
		{
			throw new NotImplementedException ();
		}

		public void ApplyMigration (Migration migration)
		{
			throw new NotImplementedException ();
		}
	}
}
