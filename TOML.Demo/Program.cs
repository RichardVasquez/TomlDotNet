using System;
using System.IO;

namespace TOML.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			const string file = "Harder.toml";
			string toml;

			using (StreamReader sr = new StreamReader(file))
			{
				toml = sr.ReadToEnd();
			}
			
			Console.WriteLine("Read the input file");
			dynamic td1, td2;
			string tds1="";
			var checkParse = TomlParser.TryParse(toml, out td1);
			if (checkParse)
			{
				Console.WriteLine("Parser says it passed, now getting ToString().");
				tds1 = td1.ToString();
			}
			else
			{
				Console.WriteLine("Parser says it failed. Hit enter to end.");
				Console.ReadLine();
				Environment.Exit(1);
			}

			Console.WriteLine("Now the resulting ToString() is going to be parsed.");
			checkParse = TomlParser.TryParse(tds1, out td2);
			if (checkParse)
			{
				Console.WriteLine("The new TOML text parsed. Getting a new ToString()");
				string tds2 = td2.ToString();
				if (tds1 == tds2)
				{
					Console.WriteLine("The two parsed texts produced the same output.");
				}
				else
				{
					Console.WriteLine("The two parsed texts did not produce the same output.");
					Console.WriteLine("Hit enter to end.");
					Console.ReadLine();
					Environment.Exit(1);
				}
			}
			else
			{
				Console.WriteLine("Parser failed on the output.  Hit enter to end.");
				Console.ReadLine();
				Environment.Exit(1);
			}

			Console.WriteLine("Initial testing passes. Hit enter to end.");
			Console.ReadLine();
			Environment.Exit(0);
		}
	}


}
