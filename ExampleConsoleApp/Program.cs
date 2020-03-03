using System;
using System.IO;
using System.Threading;

namespace ExampleConsoleApp
{
	internal class Program
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Ausstehend>")]
		private static void Main(string[] args)
		{
			TailNET tailNET = null;

			if (args.Length > 0)
			{
				try
				{
					tailNET = new TailNET(args[0]);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					Environment.Exit(1);
				}
			}
			else
			{
				Console.WriteLine("Which file do you want to monitor?");

				while (true)
				{
					string s;
					s = Console.ReadLine();
					if (File.Exists(s))
					{
						tailNET = new TailNET(s);
						break;
					}
					else
					{
						Console.WriteLine("File does not exist. Please try again.");
					}
				}
			}

			tailNET.LineAdded += TailNET_LineAdded_FIRST;
			tailNET.LineAdded += TailNET_LineAdded_SECOND;

			tailNET.Start();

			new ManualResetEventSlim(false).Wait();
		}

		private static void TailNET_LineAdded_FIRST(object sender, string e)
		{
			Int32.Parse(e);
			Console.WriteLine(e);
		}

		private static void TailNET_LineAdded_SECOND(object sender, string e)
		{
			Console.WriteLine("SECOND REACHED");
		}
	}
}