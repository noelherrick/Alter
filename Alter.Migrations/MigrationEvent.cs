using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alter.Migrations
{
	public class MigrationEvent
	{
		public string Id { get; set; }
		public int Time { get; set; }
		public DateTime DateApplied { get; set; }
		public string log { get; set; }
	}

}

