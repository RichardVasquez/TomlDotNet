using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Sprache;

namespace TOML.Demo
{
	class Program
	{
		static void Main(string[] args)
		{
			#region bleh
			string s = @"
#comment
[
#comment
	[""34"" , ""-45""],
#comment
	[true, false, false, true, true],
#comment
	[
#comment
		[
#comment
			1, #hello
#comment
			2
#comment
#comment

, #monkey
		] ,
		[
			[3],
			[4]
		],
		[5,6]
	],
	[234, 2345],
	[3.1416, 23.45,67.2, 34567.2345],
	[
		1979-05-27T07:32:00Z,
		1980-05-27T07:32:00Z,
		1968-02-29T07:32:00Z
	]
] #hahaha

#hahaha again!";
			#endregion

			string t = @"
# This is a TOML document. Boom.

title = ""TOML Example""


[owner]
name = ""Tom Preston-Werner""
organization = ""GitHub""
bio = ""GitHub Cofounder & CEO\nLikes tater tots and beer.""
dob = 1979-05-27T07:32:00Z # First class dates? Why not?

[database]
server = ""192.168.1.1""
ports = [ 8001, 8001, 8002 ]
connection_max = 5000
enabled = true

[servers]

  # You can indent as you please. Tabs or spaces. TOML don't care.
  [servers.alpha]
  ip = ""10.0.0.1""
  dc = ""eqdc10""

  [servers.beta]
  ip = ""10.0.0.2""
  dc = ""eqdc10""

[clients]
data = [ [""gamma"", ""delta""], [1, 2] ] # just an update to make sure parsers support it

# Line breaks are OK when inside arrays
hosts = [
  ""alpha"",
  ""omega""
]
";

			dynamic td;
			if(TomlParser.TryParse(t, out td));
			{
				var q = td.clients.data["hazel",0];
			}

			

			//dynamic td = new Test();
			//var v = td[ 0 ][ 1 ][ 2 ];
		}
	}


}
