Alter - Beta - Do not use in production
=

Alter is a tool for managing schema changes to a relational database that are part of application development. Its two main goals are:

- Keep database schema changes in sync with application code changes
- Provide a way to deploy changes to all environments in the same manner

It takes inspiration from Ruby on Rails, db-migrate (Java), and Entity Framework (.NET). The core idea behind the system is to create migration files in SQL that are applied in order of creation. The method of guaranteeing this is to create SQL files that begin with the number of milliseconds after the Unix epoch. This simple principle is fairly well tested with RoR and db-migrate.

The only addition that Alter provides is the idea of differential and baseline migrations. Differential migrations allow a developer to simply combine migrations into a single migration that is equivalent in effect. The need for this arises in development when a developer decides against performing a schema change. He then has to write a new migration that reverts his change. There's a distinct possibility that this change and its subsequent reversion would waste time as well as destroy data. The developer can write a differential migration that omits the change and its inverse, but includes all other changes. This way, if he’s already applied the change to his team's development environment, it will still get the reversion. The production environment will never need to waste time applying a change that only gets reverted in the next migration.

Baseline migrations allow developers to snapshot their databases for several reasons: a quick standup of a development environment or an easy review (or grep) of the tables and functions that make up a system. I am not wed to this idea, but I have seen its use on production teams.

Organization
-

### Library

The library (Alter.Migrations) enables you to write your own front-end, hook into your application initialization, application update logic, or as part of an admin for your application.

### Command-line tool

The command-line tool (Alter.CommandLine) is simply the front-end to the library. It allows you to manually or via scripts accomplish what you could do with the library (I will try to maintain feature parity between both the CommandLine and the library).

Features
-

### SQL-Based migration

Developers can use simple SQL to accomplish migrations. If the system is already created, I believe that you can create a baseline migration and Alter will only apply the changes after the migration (note that this use is not covered by tests and its currently in a workaround state. See Using with an existing database).

### Support for multiple developers

Developers can write changes independently of each other and the changes will be applied in the order that they were created. A team doesn't have to synchronize or rely on the organization skills of their DBA to make changes at the same time.

### Database status checks

Check the target database to see what version it needs.

### Dry runs

See that migrations that will be applied to a database

### Targeted migrations

In some (hopefully rare) case, you may want to specify which version you'd like to migrate to. Alter allows you to specify a migration id you'd like to target.

### Built-in & extensible database adapters

You can use the built-in Postgres adapter (more to come soon) or specify your own with ADO.NET.

### See history of migrations

The migration events are stored for your later consideration.

Design
-

Understanding the different migration types
-

A migration is a change to a database schema. The basic unit is the migration, but there are multiple ways to accomplish the migration, depending on what you'd like to accomplishing.

### Incremental

The first type is simply an incremental change. Use this when you want to make a change to your database.

### Differential

The next type is differential. Use this when you want to combine several migrations. Imagine a situation where in development, you create changes A and B. You decide to remove B, and create migration C to revert B. You can then create a new differential migration D that has only A. Those databases that are on B get C and those that are before A get D (which doesn't contain B or C, only A).

### Baseline

The baseline type is simply a snapshot of your database. It will not get applied to initialized databases. Use this when you have an existing system or if you'd like to use it quickly stand up new environments.

Using with an existing database
-

Dump your database schema and put the result in a new baseline file, noting the ID of the file that is created for you. The next steps are a workaround for the fact that this is not built into the library. Next, check the status of the database (which will create the schema table). Then, add the ID of the baseline file that you have to the schema migrations table.

Conventions
-

You don't have to use the tool for everything - although it is simple to do so. You can build your own tools and use the following conventions.
Schema Migrations
- *.sql for SQL-based migrations
- _DIFF_ in the name for migrations
- _BASELINE_ in the name for baselines

Notes
-

- All errors are hopefully UserInputExceptions or MigrationExceptions.
- Don't use 0 as a version - this is a special case that indicates a database error
- You can only go forward - never add a migration that's timestamped before the present
- Differentials are not automatic - you must manually check that the differentials are equivalent

Building & Testing
-

You need a C# 5.0 compiler and .NET Framework 4.5 runtime. You will need to use NuGet in order to get the packages. I used Mono 3.2.8 to compile and run this project.

There are two test projects: Alter.CommandLine.Tests and Alter.Migrations.Tests. Both are integration test suites and require that you have a working Postgres database. I failed to make them configurable, so you'll need to modify the source code for both.

Next steps
-

While the simple tools are often powerful, I would like Alter to a full toolkit for database developers for making changes to databases.

### Generic database adapters
I want to include support for common RDBMes: MySQL, SQLite, SQL Server, and Oracle.

### SQL-Base Baseline [Phase 3]

You can also take a snapshot of the database, where Alter dumps the schema of the database. When you take a snapshot, it is added to the migrations folder, but it is marked as a baseline. This means it will not run on databases that already have migrations applied.

### C#-based (or .NET) Migrations [Phase 3]

I'd like to create a nice interface that removes the necessity of doing diffs manually, and you can run database changes in parallel if you'd like.

### Changes verification [Phase 3]

Another feature would be checking to see if data would be compatible with current production data.


