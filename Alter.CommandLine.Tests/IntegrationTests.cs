using System;
using System.Data.Common;
using System.IO;
using System.Linq;

using Alter.CommandLine;
using Alter.Migrations;

using Dapper;
using Npgsql;
using NUnit.Framework;


namespace Alter.CommandLine.Tests
{
	[TestFixture ()]
	public class CommandLineTests
	{
		private Migrator migrator;
		private DbConnection db;
		private TextWriter errorWriter;
		private TextWriter outWriter;
		private readonly string testDb = "altertests";
		private readonly string testServer = "127.0.0.1";
		private readonly string testUser = "altertests";
		private readonly string testPassword = "altertests";

		[TestFixtureSetUp]
		public void SetUpFixture ()
		{
			db = new NpgsqlConnection ("Server="+testServer+";Port=5432;Database="+testDb+";User Id="+testUser+";Password="+testPassword+";");

			Directory.CreateDirectory (Migrator.MIGRATIONS_FOLDER);

			var adapter = new AdoNetAdapter (db);

			migrator = new Migrator (adapter);

		}

		private string getConnectionArgs () {
			return "-e postgres -h "+testServer+" -p 5432 -d "+testDb+" -u "+testUser+" -W "+testPassword;
		}

		[TestFixtureTearDown]
		public void TearDownFixture ()
		{
			foreach (var file in Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER)) {
				File.Delete (file);
			}

			Directory.Delete(Migrator.MIGRATIONS_FOLDER);

			migrator.Dispose ();
		}

		[SetUp()]
		public void SetUp () {
			errorWriter = new StringWriter ();

			Program.ErrorWriter = errorWriter;

			outWriter = new StringWriter ();

			Program.OutWriter = outWriter;
		}

		[TearDown]
		public void TearDown ()
		{
			foreach (var file in Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER)) {
				File.Delete (file);
			}

			var tables = db.Query<string> ("select table_name from information_schema.tables where table_schema = 'public';");

			foreach (var table in tables) {
				db.Execute ("drop Table " + table);
			}
		}

		[Test ()]
		public void ExitCodeZero ()
		{
			var args = new string[] { "version" };

			int exitCode = Program.Main (args);

			Assert.AreEqual (0, exitCode);
		}

		[Test ()]
		public void VersionPrintsVersion ()
		{
			var args = new string[] { "version" };

			int exitCode = Program.Main (args);

			Assert.AreEqual (0, exitCode);
			Assert.AreNotEqual(-1, outWriter.ToString().IndexOf("alter"));
		}

		[Test()]
		public void HelpPrintsHelp () {
			var args = new string[] { "help" };

			int exitCode = Program.Main (args);

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual("", errorWriter.ToString());
		}

		[Test()]
		public void AddAddsIncrementalFile () {
			var args = "add -i one".Split(' ');

			int countFilesStart = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			int exitCode = Program.Main (args);

			int countFilesEnd = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreNotEqual (Environment.NewLine, outWriter.ToString());
			Assert.AreEqual (1, countFilesEnd - countFilesStart);
		}

		[Test()]
		public void AddAddsBaselineFile () {
			var args = "add -i one -b".Split(' ');

			int countFilesStart = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			int exitCode = Program.Main (args);

			int countFilesEnd = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreNotEqual (Environment.NewLine, outWriter.ToString());
			Assert.AreEqual (1, countFilesEnd - countFilesStart);
		}

		[Test()]
		public void AddAddsDifferentialFile () {
			var args = "add -i one -D".Split(' ');

			int countFilesStart = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			int exitCode = Program.Main (args);

			int countFilesEnd = Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreNotEqual (Environment.NewLine, outWriter.ToString());
			Assert.AreEqual (1, countFilesEnd - countFilesStart);
		}

		[Test()]
		public void AddWithBothDiffAndIncrFails () {
			var args = "add -i one -I -D".Split(' ');

			int exitCode = Program.Main (args);

			Assert.AreNotEqual (0, exitCode);
			Assert.AreNotEqual (string.Empty, errorWriter.ToString());
		}

		[Test()]
		public void AddWithBothBaselineAndIncrFails () {
			var args = "add -i one -I -b".Split(' ');

			int exitCode = Program.Main (args);

			Assert.AreNotEqual (0, exitCode);
			Assert.AreNotEqual (string.Empty, errorWriter.ToString());
		}

		[Test()]
		public void AddWithBothBaselineAndDiffFails () {
			var args = "add -i one -b -D".Split(' ');

			int exitCode = Program.Main (args);

			Assert.AreNotEqual (0, exitCode);
			Assert.AreNotEqual (string.Empty, errorWriter.ToString());
		}

		[Test()]
		public void AddWithSqlWritesSql () {
			var args = "add -i one -s dummy".Split(' ');

			var sql = "select * from one";

			args[args.Length-1] = sql;

			int exitCode = Program.Main (args);

			var id = outWriter.ToString ().Replace(Environment.NewLine, "");

			var writtenSql = File.ReadAllLines (Migrator.MIGRATIONS_FOLDER + id + ".sql")[0];

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (sql, writtenSql);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
		}

		[Test()]
		public void HistoryGetsHistory () {
			migrator.AddSqlMigration ("One", "Create table one (a int);");
			migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			migrator.Migrate ();

			var args = ("history " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var output = outWriter.ToString ().Trim ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (2, output.Split (Environment.NewLine.ToCharArray()).Count ());
			Assert.AreEqual (-1, output.IndexOf ("SQL"));
			Assert.AreEqual (-1, output.IndexOf ("Log output"));
		}

		[Test()]
		public void HistoryGetsHistoryWithOptions () {
			migrator.AddSqlMigration ("One", "Create table one (a int);");
			migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			migrator.Migrate ();

			var args = ("history -l -s " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var output = outWriter.ToString ().Trim ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (6, output.Split (Environment.NewLine.ToCharArray()).Count ());
			Assert.AreNotEqual (-1, output.IndexOf ("SQL"));
			Assert.AreNotEqual (-1, output.IndexOf ("Log output"));
		}

		[Test()]
		public void DryrunGetsMigrations () {
			var mig1 = migrator.AddSqlMigration ("One", "Create table one (a int);");
			var mig2 = migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			var args = ("dryrun " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var output = outWriter.ToString ().Trim ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (2, output.Split (Environment.NewLine.ToCharArray()).Count ());
			Assert.AreNotEqual (-1, output.IndexOf (mig1));
			Assert.AreNotEqual (-1, output.IndexOf (mig2));
		}

		[Test()]
		public void DryrunGetsMigrationsWithSql () {
			var mig1 = "Create table one (a int);";
			var mig2 = "Alter table one add column b int;";
			migrator.AddSqlMigration ("One", mig1);
			migrator.AddSqlMigration ("Two", mig2);

			var args = ("dryrun " + getConnectionArgs() + " -s").Split(' ');

			int exitCode = Program.Main (args);

			var output = outWriter.ToString ().Trim ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (6, output.Split (Environment.NewLine.ToCharArray()).Count ());
			Assert.AreNotEqual (-1, output.IndexOf (mig1));
			Assert.AreNotEqual (-1, output.IndexOf (mig2));
		}

		[Test()]
		public void DryrunGetsDoesntGetAppliedMigrations () {
			migrator.AddSqlMigration ("One", "Create table one (a int);");
			migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			migrator.Migrate ();

			var args = ("dryrun " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (string.Empty, outWriter.ToString ());
		}

		[Test()]
		public void DryrunGetsMigrationsDoesntApplyThem () {
			migrator.AddSqlMigration ("One", "Create table one (a int);");
			migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			var args = ("dryrun " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var output = outWriter.ToString ().Trim ();

			var tables = db.Query<string> ("select table_name from information_schema.tables where table_schema = 'public';");

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (2, output.Split (Environment.NewLine.ToCharArray()).Count ());
			Assert.AreEqual (1, tables.Count()); // Only the migration table exists
		}

		[Test()]
		public void MigrateMigrates () {
			migrator.AddSqlMigration ("One", "Create table one (a int);");
			var mig2 = migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			var args = ("migrate " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var status = migrator.GetDatabaseVersion ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (string.Empty, outWriter.ToString());
			Assert.AreEqual (mig2, status);
		}

		[Test()]
		public void MigrateTargetedMigrates () {
			var mig1 = migrator.AddSqlMigration ("One", "Create table one (a int);");
			migrator.AddSqlMigration ("Two", "Alter table one add column b int;");

			var args = ("migrate -t " + mig1 + " " + getConnectionArgs()).Split(' ');

			int exitCode = Program.Main (args);

			var status = migrator.GetDatabaseVersion ();

			Assert.AreEqual (0, exitCode);
			Assert.AreEqual (string.Empty, errorWriter.ToString());
			Assert.AreEqual (string.Empty, outWriter.ToString());
			Assert.AreEqual (mig1, status);
		}
	}
}

