using System;

namespace Alter.Migrations
{
	public class TextMigration : Migration
	{
		public string Sql {get;set;}
		public int Time {get;set;}
		public string Id {get;set;}
	}
}