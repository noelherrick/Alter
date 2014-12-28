using System;
using Npgsql;
using System.Data.Common;

namespace Alter.Migrations
{
	public class PostgresAdapter : DatabaseAdapter
	{
		private string log = "";
		public string InfoMessage {
			get {return log; }
		}

		public DbCommand BuildCommand ()
		{
			return new NpgsqlCommand();
		}

		private NpgsqlConnection connection;

		public PostgresAdapter (string connectionString)
		{
			connection = new NpgsqlConnection (connectionString);
		}

		public DbConnection Connection
		{
			get { return connection; }
		}
			
		public string GetNativeBaseline ()
		{
			throw new NotImplementedException ();
		}
	}
}

