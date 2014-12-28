using System;
using System.Configuration;
using Alter.Migrations;
using Npgsql;

namespace Alter.Example
{
	class Runner
	{
		public static void Main (string[] args)
		{
			var appSettings = ConfigurationManager.AppSettings;

			string connString = appSettings ["AlterExampleConnection"];

			var db = new PostgresAdapter (connString);

			using (var migrator = new Migrator (db)) {
				var result = migrator.GetDatabaseVersion ();

				Console.WriteLine (result);

				migrator.GetMigrationHistory ();

				var neededMigrations = migrator.GetNeededMigrations ("3_DIFF_Three");

				foreach (var mig in neededMigrations) {
					Console.WriteLine (mig.Id);
				}

				migrator.Migrate ();

				Console.ReadKey ();
			}
		}
	}
}
