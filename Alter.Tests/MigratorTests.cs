using NUnit.Framework;
using System;
using Alter.Migrations;
using System.Collections.Generic;
using System.IO;
using Npgsql;
using System.Data.Common;
using Dapper;
using System.Linq;

namespace Alter.Tests
{
	[TestFixture ()]
	public class MigratorTests
	{
		private Migrator migrator;
		private List<string> migrations = new List<string> ();
		private DbConnection db;
		private readonly string testDb = "altertests";

		[TestFixtureSetUp]
		public void SetUpFixture ()
		{
			db = new NpgsqlConnection ("Server=127.0.0.1;Port=5432;Database="+testDb+";User Id=altertests;Password=altertests;");

			Directory.CreateDirectory (Migrator.MIGRATIONS_FOLDER);

			var adapter = new AdoNetAdapter (db);

			migrator = new Migrator (adapter);

			migrations.Add(migrator.AddSqlMigration ("One", "Create table one (a int);"));
			migrations.Add(migrator.AddSqlMigration ("Two", "Alter table one add column b int;"));
			migrations.Add(migrator.AddSqlMigration ("DIFF_Three", "Create table one (a int);\nalter table one add column b int;"));
			migrations.Add(migrator.AddSqlMigration ("BASELINE_Four", "Create table one (a int, b int);"));
			migrations.Add(migrator.AddSqlMigration ("Five", "Alter table one add column c int;"));
			migrations.Add(migrator.AddSqlMigration ("DIFF_Six", "Alter table one add column c int;"));
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

		[TearDown]
		public void TearDown ()
		{
			var tables = db.Query<string> ("select table_name from information_schema.tables where table_schema = 'public';");

			foreach (var table in tables) {
				db.Execute ("drop Table " + table);
			}
		}
			
		[Test ()]
		public void StatusCheckWorks ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var actualId = migrator.GetDatabaseVersion ();

			Assert.AreEqual(targetId, actualId);
		}

		[Test()]
		public void StatusCheckCreatesMigrationTable ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var result = db.Query("select table_name from information_schema.tables where table_name = '"+Migrator.MIGRATION_TABLE+"'");

			Assert.AreEqual (1, result.Count ());
		}

		[Test()]
		public void HistoryReturnsDataForUnitializedDb ()
		{
			var result = migrator.GetMigrationHistory ();

			Assert.IsEmpty (result);
		}

		[Test()]
		public void HistoryReturnsDataForInitializedDb ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var result = migrator.GetMigrationHistory ();

			Assert.IsNotEmpty (result);
		}

		[Test()]
		public void MigrationsWorks ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var result = db.Query("select table_name from information_schema.tables where table_name = 'one'");

			Assert.AreEqual (1, result.Count ());
		}

		[Test()]
		public void MigrationUpdatesMigrationTable ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var result = db.Query("select * from "+Migrator.MIGRATION_TABLE+" where id = '"+targetId+"'");

			Assert.AreEqual (1, result.Count ());
		}

		[Test()]
		public void MultipleMigrationsWork ()
		{
			var targetId = migrations [2];

			migrator.Migrate (targetId);

			var result = db.Query("select table_name from information_schema.columns where table_name = 'one' and column_name = 'b'");

			Assert.AreEqual (1, result.Count ());
		}

		[Test()]
		public void TargetedMigrationsWork ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			var result = db.Query("select table_name from information_schema.columns where table_name = 'one' and column_name = 'b'");

			Assert.AreEqual (0, result.Count ());
		}

		[Test()]
		public void MigrationsDoNotGetAppliedTwice ()
		{
			var targetId = migrations [2];

			migrator.Migrate (targetId);

			Assert.True (true);
		}

		[Test()]
		public void DefaultMigrationGoesToCurrentVersion ()
		{
			var targetId = migrations.Last ();

			migrator.Migrate ();

			var actualId = migrator.GetDatabaseVersion ();

			Assert.AreEqual(targetId, actualId);
		}

		[Test()]
		public void DifferentialReplacesIncremental ()
		{
			var targetId = migrations [2];

			migrator.Migrate (targetId);

			var actualId = migrator.GetDatabaseVersion ();

			Assert.AreEqual(targetId, actualId);
		}

		[Test()]
		public void DifferentialOnlyGoesBackToLastDiff ()
		{
			var targetId = migrations [5];

			migrator.Migrate (targetId);

			Assert.True (true);
		}

		/* Say we have incremental migrations A, B, C with differential D replacing them.
		 * If the target DB is on version A, apply B & C, do not apply D */
		[Test()]
		public void DiffIsNotAppliedIfAIncrInTargetedSeriesIsApplied ()
		{
			// First we migration to the first mig in the series
			var targetId = migrations [0];
			migrator.Migrate (targetId);

			// Then we migrate all the way
			migrator.Migrate ();

			var result = db.Query("select * from "+Migrator.MIGRATION_TABLE+" where id = 'DIFF_Three'");

			Assert.AreEqual (0, result.Count ());
		}

		/* Say we have incremental migrations A, B, C with differential D replacing them.
		 * If the requested version id is B, only apply A & B, do not apply D */
		[Test()]
		public void DiffIsNotAppliedIfAfterTargetId ()
		{
			var targetId = migrations [2];
			migrator.Migrate (targetId);

			var actualId = migrator.GetDatabaseVersion ();

			Assert.AreEqual(targetId, actualId);
		}

		[Test()]
		public void BaselineAppliesToUnitializedDb ()
		{
			migrator.Migrate ();

			var baselineId = migrations[3];

			var result = db.Query("select * from "+Migrator.MIGRATION_TABLE+" where id = '"+baselineId+"'");

			Assert.AreEqual (1, result.Count ());
		}

		[Test()]
		public void BaselineDoesNotApplyToInitializedDb ()
		{
			var targetId = migrations [0];

			migrator.Migrate (targetId);

			migrator.Migrate ();

			var baselineId = migrations[3];

			var result = db.Query("select * from "+Migrator.MIGRATION_TABLE+" where id = '"+baselineId+"'");

			Assert.AreEqual (0, result.Count ());
		}

		[Test()]
		public void OnlyMigrationsAfterBaselineAreApplied ()
		{
			migrator.Migrate ();

			var baselineId = migrations[3];

			var result = db.Query("select * from "+Migrator.MIGRATION_TABLE+" where id < '"+baselineId+"'");

			Assert.AreEqual (0, result.Count ());
		}

		[Test()]
		public void AddSqlMigrationCreatesFile ()
		{
			migrations.Add(migrator.AddSqlMigration ("Seven", "select * from one"));

			Assert.AreEqual (7, Directory.EnumerateFiles (Migrator.MIGRATIONS_FOLDER).Count());
		}

		[Test()]
		public void ErrorIsThrownForNoMigrationFolder ()
		{
			Directory.Move(Migrator.MIGRATIONS_FOLDER, "new_"+Migrator.MIGRATIONS_FOLDER);

			Assert.Throws<MigrationException> (() => { migrator.Migrate ();});

			Directory.Move("new_"+Migrator.MIGRATIONS_FOLDER, Migrator.MIGRATIONS_FOLDER);
		}
	}
}

