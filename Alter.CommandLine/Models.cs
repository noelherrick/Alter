using System;
using CommandLineLib = CommandLine;
using CommandLine.Text;
using System.Diagnostics;
using System.Reflection;

namespace Alter.CommandLine
{
	public class ConnectionOptions : DefaultOptions {
		[CommandLineLib.Option('h', "host", Required = true,
			HelpText = "The database server.")]
		public string Host { get; set; }

		[CommandLineLib.Option('p', "port", Required = true,
			HelpText = "The database server port.")]
		public string Port { get; set; }

		[CommandLineLib.Option('d', "database", Required = true,
			HelpText = "The database name.")]
		public string Database { get; set; }

		[CommandLineLib.Option('u', "user", Required = true,
			HelpText = "The database user.")]
		public string User { get; set; }

		[CommandLineLib.Option('W', "password", Required = true,
			HelpText = "The database password.")]
		public string Password { get; set; }

		[CommandLineLib.Option('e', "engine", Required = true,
			HelpText = "The database engine (oracle, mysql, postgres, sqlserver, sqlite).")]
		public string Engine { get; set; }
	}

	public class DefaultOptions {
		private static readonly string SUMMARY =
			@"The Alter tool uses simple SQL files to keep your database schema in sync with your application. You write migration files and then Alter applies them to the database, keeping track of which migrations have already been applied.";

		[CommandLineLib.Option('v', "verbose", DefaultValue = false,
			HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[CommandLineLib.ParserState]
		public CommandLineLib.IParserState LastParserState { get; set; }

		[CommandLineLib.HelpOption]
		public virtual string GetUsage()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly ();

			var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

			var help = new HelpText {
				Heading = new HeadingInfo(asm.GetName().Name, asm.GetName().Version.ToString()),
				AdditionalNewLineAfterOption = true,
				AddDashesToOption = true };
			help.AddPreOptionsLine ("Copyright (c) " + versionInfo.LegalCopyright);
			help.AddPreOptionsLine(SUMMARY);
			help.AddPreOptionsLine (String.Empty);
			help.AddPreOptionsLine ("Commands");
			help.AddPreOptionsLine (String.Empty);
			foreach (var cmd in Controller.GetCommandHelp()) {
				help.AddPreOptionsLine (cmd);
			}
			help.AddOptions(this);
			return help;
		}
	}

	class AddOptions : DefaultOptions {
		[CommandLineLib.Option('D', "diff",
			HelpText = "Creates an new differential migration.")] //, MutuallyExclusiveSet = "type"
		public bool Diff { get; set; }

		[CommandLineLib.Option('I', "incremental",
			HelpText = "Creates an new incremental migration.")]
		public bool Incr { get; set; }

		[CommandLineLib.Option('b', "baseline",
			HelpText = "Creates an new baseline migration.")]
		public bool Baseline { get; set; }

		[CommandLineLib.Option('i', "id", Required = true,
			HelpText = "Migration id.")]
		public string Id { get; set; }

		[CommandLineLib.Option('s', "sql",
			HelpText = "Migration SQL.")]
		public string Sql { get; set; }
	}

	public class HistoryOptions : DefaultOptions {
		[CommandLineLib.Option('s', "sql",
			HelpText = "Migration SQL.")]
		public bool Sql { get; set; }
		[CommandLineLib.Option('l', "log",
			HelpText = "Print the log from the migration")]
		public bool Log { get; set; }
	}

	public class DryrunOptions : DefaultOptions {
		[CommandLineLib.Option('s', "sql",
			HelpText = "Migration SQL.")]
		public bool Sql { get; set; }
	}

	public class MigrateOptions : DefaultOptions {
		[CommandLineLib.Option('t', "target",
			HelpText = "The ID of the migration to bring the database up to.")]
		public string TargetId { get; set; }
	}
}

