using System;
using System.Data.Common;

namespace Alter.Migrations
{
	public class AdoNetAdapter : DatabaseAdapter
	{
		public bool NeedsConnection {
			get {
				return true;
			}
		}

		private DbConnection db;

		public AdoNetAdapter (DbConnection db)
		{
			this.db = db;
		}

		public System.Data.Common.DbCommand BuildCommand ()
		{
			return db.CreateCommand ();
		}

		public string GetNativeBaseline ()
		{
			throw new NotImplementedException ();
		}

		public System.Data.Common.DbConnection Connection {
			get { return db; }
		}

		public string InfoMessage {
			get {
				return "";
			}
		}
	}
}

