using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alter.Migrations
{
	/// <summary>
	/// Represents a migration event.
	/// </summary>
	public class MigrationEvent
	{
		public string Id { get; set; }
		public int Time { get; set; }
		public DateTime DateApplied { get; set; }
		public string Log { get; set; }
	}
}