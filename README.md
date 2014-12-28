Alter
=

The idea behind this project is to create a set of software to easily allow a database to be added to a project without creating an unmanageable behemoth that is a pain to update and a terror to get a testing / development environment started.

Basic features
-

### SQL-Based Migration

A migration is a change to a database schema. The basic unit is the migration, but there are multiple ways to accomplish the migration. The first type is simply an incremental change.

1. Incremental
2. Differential

#### Conventions


You don't have to use the tool for everything - although it is simple to do so. You can build your own tools and use the following conventions.
SchemaMigrations
- *.sql for SQL-based migrations
- _DIFF_ in the name for migrations
- _BASELINE_ in the name for baselines

### SQL-Base Baseline [Phase 2]

You can also take a snapshot of the database, where Alter dumps the schema of the database. When you take a snapshot, it is added to the migrations folder, but it is marked as a baseline. This means it will not run on databases that already have migrations applied.

### C#-based (or .NET) Migrations [Phase 3]

I'd like to create a nice interface that removes the necessity of doing diffs manually, and you can run database versions in parallel if you'd like.

### Command-line tool

The command-line tool is simply the front-end to the library. It allows you to manually or via scripts accomplish what you could do with the library.

### Library

The Library enables you to write your own front-end, hook into your application initialization, application update logic, or as part of an admin for your application.

All errors are hopefully MigrationExceptions