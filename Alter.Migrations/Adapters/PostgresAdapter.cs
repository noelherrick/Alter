using System;
using Npgsql;
using System.Data.Common;

namespace Alter.Migrations
{
	public class PostgresAdapter : DatabaseAdapter
	{
		public bool NeedsConnection {
			get {
				return true;
			}
		}

		private NpgsqlConnection connection;
		private string log = "";

		public PostgresAdapter (string connectionString)
		{
			connection = new NpgsqlConnection (connectionString);
		}

		public string InfoMessage {
			get {return log; }
		}

		public DbCommand BuildCommand ()
		{
			return new NpgsqlCommand();
		}

		public DbConnection Connection
		{
			get { return connection; }
		}
			
		public string GetNativeBaseline ()
		{
			throw new NotImplementedException ();
		}

		public static string BuildConnectionString (ConnectionProperties properties)
		{
			var p = properties;

			p.Port = p.Port == string.Empty ? "5432" : p.Port;

			return string.Format("Server={0};Port={1};Database={2};User Id={3};Password={4};", p.Server, p.Port, p.Database, p.Username, p.Password);
		}
	}
}

