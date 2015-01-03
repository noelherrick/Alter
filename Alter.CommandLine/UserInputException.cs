using System;

namespace Alter.CommandLine
{
	public class UserInputException : Exception
	{
		public UserInputException (string message) : base (message) {
		}
	}
}