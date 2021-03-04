using System;
using System.IO;
using System.Threading;

namespace ExampleConsoleApp
{
	internal class Program
	{
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

			tailNET.Start();

			new ManualResetEventSlim(false).Wait();
		}

		private static void TailNET_LineAdded_FIRST(object sender, string e)
		{
			Console.WriteLine(e);
		}
	}
}