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

	
			var cd = td.clients.data;
			var tdmn = td.GetDynamicMemberNames();
			var cdmn = cd.GetDynamicMemberNames();
			var tdth = td.GetTreeHash();
			var tdfh = td.GetFlatHash();
			var cdth = cd.GetTreeHash();
			var cdfh = cd.GetFlatHash();

			string s = td.ToString();
		}
	}


}
