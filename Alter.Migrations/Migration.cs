using System;

namespace Alter.Migrations
{
	public interface Migration
	{
		string Id {
			get;
			set;
		}
		int Time {
			get;
			set;
		}
		string Sql {
			get;
		}
	}
}