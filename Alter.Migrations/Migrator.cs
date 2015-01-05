using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using System.Data.Common;
using System.Data.SqlClient;
using Npgsql;
using System.Data;


namespace Alter.Migrations
{
	/// <summary>
	/// The Migrator class wrapps all the corenfunctionality of Alter.
	/// </summary>
	public class Migrator : IDisposable
	{
		/// <summary>
		/// The name of the migration table that will be created in the target database.
		/// The database user used to connect to the target needs permission to create,
		/// select and insert into this table.
		/// </summary>
		public static readonly string MIGRATION_TABLE = "_schema_migrations";

		/// <summary>
		/// The name of the folder that will contain the migrations.
		/// </summary>
		public static readonly string MIGRATIONS_FOLDER = "SchemaMigrations/";

		/// <summary>
		/// The Alter API version. Uses semantic versioning.
		/// </summary>
		public static readonly string ALTER_API_VERSION = "0.1.1";
		private readonly string UNINITIALIZED_VERSION_ID = "0";
		private DatabaseAdapter db;
		private static long lastId = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="Alter.Migrations.Migrator"/> class.
		/// </summary>
		/// <param name="db">The database adapter that connects to the target database.</param>
		public Migrator (DatabaseAdapter db)
		{
			this.db = db;

			if (db.NeedsConnection && db.Connection.State == ConnectionState.Closed) {
				try {
					db.Connection.Open ();
				} catch (Exception e) {
					throw new Exception ("Could not open a connection to the database. See inner exception for more details.", e);
				}
			}
		}

		public Migrator ( ConnectionProperties properties)
			: this(GetAdapter(properties)) {

		}

		public static long ToUnixTime(DateTime date)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt64((date - epoch).TotalSeconds);
		}

		private static DatabaseAdapter GetAdapter (ConnectionProperties properties) {
			var adapterType = properties.Engine;

			if (adapterType == "postgres") {

				var connectionString = properties.ConnectionString != null ?
					properties.ConnectionString :
					PostgresAdapter.BuildConnectionString (properties);

				var adapter = new PostgresAdapter (connectionString);
				return (adapter);
			} else if (adapterType == "sqlserver") {
				throw new NotImplementedException ("The sqlserver adapter is not implemented.");
			} else if (adapterType == "oracle") {
				throw new NotImplementedException ("The oracle adapter is not implemented.");
			} else if (adapterType == "sqlite") {
				throw new NotImplementedException ("The sqlite adapter is not implemented.");
			} else if (adapterType == "mysql") {
				throw new NotImplementedException ("The mysql adapter is not implemented.");
			} else {
				throw new MigrationException ("No adapter named " + adapterType + " was found.", null);
			}
		}

		/// <summary>
		/// Gets the database version (the ID of the last migration that was applied).
		/// Connects to the database to read the migration table
		/// </summary>
		/// <returns>The database version.</returns>
		public string GetDatabaseVersion ()
		{
			string dbVersionId = UNINITIALIZED_VERSION_ID;

			if (verifyOrCreateMigrationTable ()) {
				var results = db.Connection.Query<string> ("select Id from " + MIGRATION_TABLE + " order by Id desc limit 1");

				if (results.Count () == 1) {
					dbVersionId = results.First ();
				}
			}

			return dbVersionId;
		}

		/// <summary>
		/// Verifies or creates the migration table.
		/// This function should be used before querying the migration table.
		/// </summary>
		/// <returns><c>true</c>, if or create migration table was already present,
		/// <c>false</c> if it was created.</returns>
		private bool verifyOrCreateMigrationTable () {
			try {
				// Check if the table exists first
				if (db.Connection.Query("select * from information_schema.tables where table_name = '"+MIGRATION_TABLE+"';").Count() == 1)
				{
					return true;
				}
				else
				{
					// Create the table if it doesn't
					var tableSql = "Create table "+MIGRATION_TABLE+@"
							(
								id varchar(100) not null primary key,
								date_applied date not null,
								time_taken integer not null,
								log varchar(2000) not null
							)";

					try {
						db.Connection.Execute (tableSql);
					} catch (SqlException e) {
						throw new MigrationException ("Could not create migration table. See inner exception for more details.", e);
					}

					return false;
				}
			} catch (SqlException e) {
				throw new MigrationException ("Could not query database to find migration table. See inner exception for more details.", e) ;
			}
		}

		/// <summary>
		/// Gets all the migration events for the target database.
		/// </summary>
		/// <returns>The migration history.</returns>
		public IEnumerable<MigrationEvent> GetMigrationHistory ()
		{
			IEnumerable<MigrationEvent> events = new List<MigrationEvent> ();

			if (verifyOrCreateMigrationTable ()) {
				try {
					events = db.Connection.Query<MigrationEvent> ("Select * from " + MIGRATION_TABLE);
				} catch (SqlException e) {
					throw new MigrationException ("Could not query migration table. See inner exception for more details.", e) ;
				}
			}
			
			return events;
		}

		/// <summary>
		/// Creates a new SQL-based migration in the migrations folder.
		/// Generates a random ID that is guarenteed to be greater than the previous migrations.
		/// </summary>
		/// <returns>The generated ID of the migration.</returns>
		/// <param name="description">The description of the migration.</param>
		/// <param name="sql">The SQL to perform the migration (Optional).</param>
		public string AddSqlMigration (string description, string sql = "", MigrationType type = MigrationType.INCREMENTAL)
		{
			//long millis = ToUnixTime(DateTime.Now);
			long millis = DateTime.Now.Ticks;

			var tag = "";

			if (description.IndexOf("DIFF") != -1) {
				throw new MigrationException ("The migration description cannot contain DIFF.", null);
			}

			if (description.IndexOf("BASELINE") != -1) {
				throw new MigrationException ("The migration description cannot contain BASELINE.", null);
			}

			if (type.Equals(MigrationType.DIFFERENTIAL)) {
				tag = "_DIFF";
			} else if (type.Equals(MigrationType.BASELINE)){
				tag = "_BASELINE";
			}

			var id = string.Format ("{0}{1}_{2}", millis, tag, description);
			var path = MIGRATIONS_FOLDER+id+".sql";

			try {
				using (var sw = new StreamWriter (path)) {
					sw.Write (sql);
				}

				return id;
			} catch (Exception e) {
				throw new MigrationException ("Could not create a new migration file. See inner exception for more details.", e);
			}
		}

		/// <summary>
		/// Adds a new baseline after reading the target database's schema.
		/// </summary>
		public void AddBaseline ()
		{
			throw new NotImplementedException ("This is function is not implemented - it will in Phase 2");
		}

		/// <summary>
		/// Gets the target database's current version and returns all migrations
		/// that come after.
		/// </summary>
		/// <returns>The needed migrations.</returns>
		/// <param name="targetId">Target identifier.</param>
		public IEnumerable<Migration> GetNeededMigrations (string targetId = "")
		{
			var neededMigrations = new List<Migration> ();

			var dbVersionId = GetDatabaseVersion ();

			// Only look at migrations until the version specified by targetId 
			var availableMigrations = GetAvailableMigrations ().Where(x => x.Id.CompareTo(targetId) != 1 || targetId == "");

			var migrationCache = new List<Migration> ();

			foreach (var migration in availableMigrations)
			{

				if (migration.Id.Contains ("_DIFF_")) {

					bool isLastMigrationDiff = dbVersionId.Contains ("_DIFF_");

					// We don't need any previous incremental migrations except if any incrementals were applied
					if (!isLastMigrationDiff &&
						migrationCache.Exists(x => x.Id == dbVersionId) &&
						migrationCache.Where(x => x.Id.CompareTo(dbVersionId) != -1).Count() > 0
					) {
						neededMigrations.AddRange (migrationCache);
						migrationCache = new List<Migration> ();
					}
					else {
						migrationCache = new List<Migration> ();

						neededMigrations.Add (migration);
					}

				} else if (migration.Id.Contains ("_BASELINE_")) {
					// Don't apply the baseline unless the DB is at version 0
					if (dbVersionId == UNINITIALIZED_VERSION_ID)
					{
						neededMigrations = new List<Migration> ();
						neededMigrations.Add (migration);
					}
				} else {
					migrationCache.Add (migration);
				}
			}

			neededMigrations.AddRange (migrationCache);

			// Only return migrations those after the current version
			return neededMigrations.Where(x => x.Id.CompareTo(dbVersionId) == 1);
		}

		/// <summary>
		/// Reads all the schema migrations folder and returns their paths.
		/// </summary>
		/// <returns>The available migrations.</returns>
		private IEnumerable<Migration> GetAvailableMigrations ()
		{
			var migs = new List<Migration> ();

			try {
				var migrationFiles = Directory.GetFiles(MIGRATIONS_FOLDER).OrderBy(x => x);

				foreach (var file in migrationFiles)
				{
					try {
						using (var sr = new StreamReader(file))
						{
							var sql = sr.ReadToEnd();

							var id = file.Replace (".sql", "").Replace (MIGRATIONS_FOLDER, "");

							if (id == UNINITIALIZED_VERSION_ID) {
								throw new InvalidOperationException ("You cannot use 0 as a migration ID (i.e., you cannot name a migration file 0.sql).");
							}

							migs.Add (new TextMigration(){Id =id, Sql = sql});
						}
					} catch (Exception e) {
						throw new MigrationException ("Error reading migration file ("+file+"). See inner exception for more details.", e);
					}
				}

				return migs;
			} catch (Exception e) {
				throw new MigrationException ("Error reading migration folder. See inner exception for more details.", e);
			}

		}

		/// <summary>
		/// Migrates the database to the latest version or the passed-in target id.
		/// </summary>
		/// <param name="targetId">The version id to migrate the database to (Optional).</param>
		public IEnumerable<MigrationEvent> Migrate (string targetId = "")
		{
			var results = new List<MigrationEvent> ();

			var neededMigrations = GetNeededMigrations (targetId);

			var xact = db.Connection.BeginTransaction ();

			try {
				foreach (var mig in neededMigrations) {
					var log = "";

					DbCommand cmd = db.BuildCommand();
					cmd.Connection = db.Connection;
					cmd.Transaction = xact;
					cmd.CommandText = mig.Sql;

					var startTime = DateTime.Now;

					int rowsAffected = cmd.ExecuteNonQuery();

					var endTime = DateTime.Now;

					var timeTaken = endTime - startTime;

					if (rowsAffected != -1)
					{
						log += rowsAffected + " row(s) affected.";
					}

					log += db.InfoMessage;

					var @event = new MigrationEvent(){ Id=mig.Id, Log = log, Time = timeTaken.Milliseconds, DateApplied = DateTime.Now };

					db.Connection.Execute ("insert into " + MIGRATION_TABLE + " (id, Date_applied, log, time_taken) values (@id, NOW(), @log, @time)", @event);

					results.Add(@event);
				}

				xact.Commit ();
			}
			catch (Exception e) {
				xact.Rollback ();
				throw e;
			}
				
			return results;
		}

		public void Dispose ()
		{
			if (db.Connection.State != ConnectionState.Closed) {
				db.Connection.Close ();
			} 
		}
	}
}

