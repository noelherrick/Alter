using System;
using CommandLine.Text;
using System.Collections.Generic;
using Alter.Migrations;

namespace Alter.CommandLine
{
	public class Program
	{
		private delegate string Command (IDictionary<string, string> args);

		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
		}
	}

	public class CommandTool
	{
		private static readonly string TOOL_NAME = "Alter Command Line Tool";
		private static readonly string VERSION_ID = "0.1.1";
		private static readonly string SUMMARY =
		@"The Alter tool uses simple SQL files to keep your database schema in sync with your application.
		You write migration files and then Alter applies them to the database, keeping track of which
		migrations have already been applied.";
		private static readonly string EXAMPLES = @"";

		public string Version (IDictionary<string, string> args) {
			return TOOL_NAME + " (" + VERSION_ID + ")";
		}

		public string Help (IDictionary<string, string> args) {
			return TOOL_NAME + " (" + VERSION_ID + ")\n" + SUMMARY + EXAMPLES;
		}

		public string Add (IDictionary<string, string> args) {
			var migrator = new Migrator ();

			var migrationType = MigrationType.INCREMENTAL;

			if (args.ContainsKey ("--diff")) {
				migrationType = MigrationType.DIFFERENTIAL;
			} else if (args.ContainsKey ("--baseline")) {
				migrationType = MigrationType.BASELINE;
			}

			migrator.AddSqlMigration (args ["--id"], args ["--sql"]);

			return "";
		}
	}
}
