using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Alter.Migrations
{
	public interface DatabaseAdapter
	{

		DbConnection Connection { get; }
		DbCommand BuildCommand ();
		string GetNativeBaseline ();
		string InfoMessage { get; }

		// These are for snapshotting the database
		/*
		IEnumerable<Table> GetTables ();
		IEnumerable<View> GetViews ();
		IEnumerable<StoredProcedure> GetStoredProcedures ();
		IEnumerable<StoredFunction> GetStoredFunctions ();
		IEnumerable<Permission> GetPermissions ();
		*/
	}
}