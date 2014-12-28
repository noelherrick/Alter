using System;
using System.Collections.Generic;

namespace Alter.Migrations
{
	public class Permission
	{
		public string Object { get; set; }
		public string Grantee { get; set; }
	}
}