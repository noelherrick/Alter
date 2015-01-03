﻿using System;
using System.IO;
using Alter.Migrations;
using System.Linq;
using System.Configuration;

namespace Alter.CommandLine
{
	public class Program
	{
		public static TextWriter ErrorWriter = Console.Error;
		public static TextWriter OutWriter = Console.Out;

		private static void errorPrinter (Exception exp, bool verbose) {
			ErrorWriter.WriteLine (exp.Message);
			if (exp.InnerException != null) {
				errorPrinter (exp.InnerException, verbose);
			}

			if (verbose) {
				ErrorWriter.WriteLine (exp.StackTrace);
			}
		}

		public static int Main (string[] args)
		{
			bool verbose = args.Contains ("-v");

			ConnectionProperties connProps = null;

			if (File.Exists("app.config")) {
				File.Copy ("app.config", "alter.exe.config", true);

				var appSettings = ConfigurationManager.AppSettings;

				string connString = appSettings ["AlterConnection"];
				string connEngine = appSettings ["AlterConnectionEngine"];

				if (connString != string.Empty) {
					connProps = new ConnectionProperties() {ConnectionString = connString, Engine = connEngine};
				}
			}

			try {
				var controller = new Controller();

				Controller.SetCommandLineWriter(ErrorWriter);

				var result = controller.Dispatch(args, connProps);

				OutWriter.Write(result);

				return 0;
			} catch (UserInputException e) {
				ErrorWriter.WriteLine (e.Message);
				OutWriter.WriteLine (Controller.Help(args, null));
				return 1;
			} catch (MigrationException e) {
				ErrorWriter.WriteLine ("There was a migration exception. This could be caused by a problem with your client or server.");
				errorPrinter (e, verbose);
				return 2;
			} catch (Exception e) {
				ErrorWriter.WriteLine ("There was an uncaught exception. This could be caused by a problem with your client, server, or the code.");
				errorPrinter (e, verbose);
				return 3;
			}
		}
	}
}
