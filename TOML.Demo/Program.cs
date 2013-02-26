using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Sprache;

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

			dynamic td;
			var checkParse = TomlParser.TryParse(toml, out td);

	
			var q = td.clients.data;
			var s = td.GetDynamicMemberNames();
			var t = q.GetDynamicMemberNames();
			var h1 = td.GetTreeHash();
			var h2 = td.GetFlatHash();
		}
	}


}
