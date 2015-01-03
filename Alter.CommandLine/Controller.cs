using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alter.Migrations;

using CommandLineLib = CommandLine;
using CommandLine.Text;
using System.IO;

namespace Alter.CommandLine
{
	public class Controller
	{
		private static CommandLineLib.Parser parser = new CommandLineLib.Parser (with => {
			with.IgnoreUnknownArguments = true;
		});

		public static void SetCommandLineWriter (TextWriter commandLineTextWriter) {
			parser = new CommandLineLib.Parser (with => {
				with.HelpWriter = commandLineTextWriter;
				with.IgnoreUnknownArguments = true;
			});
		}
			
		public string Dispatch (string[] args, ConnectionProperties connectionProperties) {
			if (args.Length < 1) {
				throw new UserInputException ("Please specify a command");
			} else {
				var cmdName = args [0];

				if (Commands.ContainsKey (cmdName)) {
					var cmd = Commands [cmdName];

					if (cmd.ConnectionNeeded && connectionProperties == null) {
						connectionProperties = extractConnectionProperties (args);
					}

					return cmd.Action(args, connectionProperties);
				} else {
					throw new UserInputException (cmdName + " command not found. The command must be the first argument.");
				}
			}
		}

		//private static readonly string EXAMPLES = @"";

		public delegate string  CommandAction (string[] args, ConnectionProperties connectionProperties);

		public class Command
		{
			public Command (CommandAction action, string summary, bool connectionNeeded = false)
			{
				Action = action;
				Summary = summary;
				ConnectionNeeded = connectionNeeded;
			}

			public CommandAction Action { get; set; }
			public string Summary { get; set; }
			public bool ConnectionNeeded { get; set; }
		}

		public static IEnumerable<string> GetCommandHelp ()
		{
			var commandStrings = new List<string> ();

			foreach (var cmd in Commands) {
				var sb = new StringBuilder ();
				sb.Append (cmd.Key);
				sb.Append (" - ");
				sb.Append (cmd.Value.Summary);
				commandStrings.Add (sb.ToString());
			}

			return commandStrings;
		}

		public readonly static IDictionary<string, Command> Commands = new Dictionary<string, Command> {
			{"help" , new Command(Help, "gets the options for this tool")},
			{"version" , new Command(Version, "prints the version of the tool")},
			{"add" , new Command(Add, "adds a new migration")},
			{"status" , new Command(Status, "gets the status of the target database", true)},
			{"history" , new Command(History, "shows the migration events for the target database", true)},
			{"dryrun" , new Command(Dryrun, "shows the migrations available to apply", true)},
			{"migrate" , new Command(Migrate, "performs migrations on the target database", true)},
		};

		private static ConnectionProperties extractConnectionProperties (string[] args) {
			var opts = getOptions<ConnectionOptions> (args);

			return new ConnectionProperties () {
				Engine = opts.Engine,
				Server = opts.Host,
				Port = opts.Port,
				Database = opts.Database,
				Username = opts.User,
				Password = opts.Password
			};
		}

		private static T getOptions<T> (string[] args) where T : new() {
			var options = new T ();

			parser.ParseArguments (args, options);

			return options;
		}
			
		public static string Version (string[] args, ConnectionProperties connectionProperties) {
			var asm = System.Reflection.Assembly.GetExecutingAssembly ();

			return asm.GetName().Name + " (" + asm.GetName().Version.ToString() + ")";
		}

		public static string Help (string[] args, ConnectionProperties connectionProperties) {
			var opts = new DefaultOptions ();

			return opts.GetUsage ();
		}
			
		public static string Add (string[] args, ConnectionProperties connectionProperties) {
			var opts = getOptions<AddOptions> (args);

			var migrator = new Migrator (new ConsoleAdapter());

			var migrationType = MigrationType.INCREMENTAL;

			if ((opts.Baseline && opts.Incr) || (opts.Diff && opts.Incr) || (opts.Baseline && opts.Diff)) {
				throw new UserInputException ("You must only specify one migration file type: Baseline, Diff, or Incr");
			}

			if (opts.Diff) {
				migrationType = MigrationType.DIFFERENTIAL;
			} else if (opts.Baseline) {
				migrationType = MigrationType.BASELINE;
			}

			return migrator.AddSqlMigration (opts.Id, opts.Sql, migrationType);
		}

		private static ConnectionProperties buildConnProps (ConnectionOptions opts) {
			return new ConnectionProperties () {
				Server = opts.Host,
				Port = opts.Port,
				Database = opts.Database,
				Username = opts.User,
				Password = opts.Password
			};
		}

		public static string Status (string[] args, ConnectionProperties connectionProperties) {
			var migrator = new Migrator (connectionProperties);

			return migrator.GetDatabaseVersion ();
		}

		public static string History (string[] args, ConnectionProperties connectionProperties) {
			var opts = getOptions<HistoryOptions> (args);

			var migrator = new Migrator (connectionProperties);
 
			var events = migrator.GetMigrationHistory ();

			var sb = new StringBuilder();

			foreach (var e in events) {
				sb.Append(e.Id).Append(" ").Append(e.DateApplied).Append(Environment.NewLine);

				if (opts.Sql) {
					using (var sr = new StreamReader(Migrator.MIGRATIONS_FOLDER + e.Id + ".sql"))
					{
						var sql = sr.ReadToEnd();
						sb.Append("SQL").Append(Environment.NewLine).Append(sql);
					}
				}

				if (opts.Log) {
					sb.Append("Log output").Append(Environment.NewLine).Append(e.Log);
				}
			}

			return sb.ToString();
		}

		public static string Dryrun (string[] args, ConnectionProperties connectionProperties) {
			var opts = getOptions<DryrunOptions> (args);

			var migrator = new Migrator (connectionProperties);

			var migrations = migrator.GetNeededMigrations ();

			var sb = new StringBuilder();

			foreach (var migs in migrations) {
				sb.Append(migs.Id).Append(Environment.NewLine);

				if (opts.Sql) {
					using (var sr = new StreamReader(Migrator.MIGRATIONS_FOLDER+migs.Id + ".sql"))
					{
						var sql = sr.ReadToEnd();
						sb.Append("SQL").Append(Environment.NewLine).Append(sql).Append(Environment.NewLine);
					}
				}
			}

			return sb.ToString();
		}

		public static string Migrate (string[] args, ConnectionProperties connectionProperties) {
			var opts = getOptions<MigrateOptions> (args);

			var migrator = new Migrator (connectionProperties);

			migrator.Migrate(opts.TargetId?? "");

			return "";
		}
	}
}

