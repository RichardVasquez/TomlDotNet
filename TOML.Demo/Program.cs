using System;
using System.IO;

namespace TOML.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			const string file = "Example.toml";
			string toml;

			using (StreamReader sr = new StreamReader(file))
			{
				toml = sr.ReadToEnd();
			}

			dynamic td1, td2;
			string s1="", s2="";

			var checkParse = TomlParser.TryParse(toml, out td1);

			if (checkParse)
			{
				Console.WriteLine("Parser says it passed.");
				var t = td1.Stuff();
			}

			//    var cd = td1.clients.data;
			//    //	Examine these in the debugger.

			//    long i = td1.database.ports[2];
			//    long j = td1["database"]["ports"][2];
			//    long k = td1["database", "ports", 2];
			//    long m = td1["database.ports"][2];

			//    var tdmn1 = td1.GetDynamicMemberNames();
			//    var cdmn1 = cd.GetDynamicMemberNames();
			//    var tdth1 = td1.GetTreeHash();
			//    var tdfh1 = td1.GetFlatHash();
			//    var cdth1 = cd.GetTreeHash();
			//    var cdfh1 = cd.GetFlatHash();
			//    s1 = td1.ToString();
			//}

			//if (!string.IsNullOrEmpty(s1))
			//{
			//    Console.WriteLine("We've turned the data into a string.");

			//    var checkParse2 = TomlParser.TryParse(s1, out td2);
			//    if (checkParse2)
			//    {
			//        Console.WriteLine("Parsing the output of the input into a new TomlDocument seems to have worked.");
			//        Console.WriteLine();
			//        Console.WriteLine();
			//        Console.WriteLine("Let's check...");

			//        s2 = td2.ToString();
			//    }

			//    if (s1 == s2)
			//    {
			//        Console.WriteLine("An original TOML document was parsed.");
			//        Console.WriteLine("A text string output was created from the resulting parsing.");
			//        Console.WriteLine("A new parsing was attempted on the resulting output, then output was compared.");
			//        Console.WriteLine("They match.  We're effectively done now.");
			//    }
			//}
			//else
			//{
			//    Console.WriteLine("Unable to create a string...");
			//}
		}
	}


}
