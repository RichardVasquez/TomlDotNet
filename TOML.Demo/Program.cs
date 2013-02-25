using System;
using System.Collections.Generic;
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
[database]
server = ""192.168.1.1""
ports = [ 8001, 8001, 8002 ]
connection_max = 5000
enabled = true";
	
			var v = TOML.Parser.NamedBlock.TryParse(t);
		}
	}
}
