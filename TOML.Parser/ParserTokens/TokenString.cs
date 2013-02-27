using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TOML.ParserTokens
{
	[DebuggerDisplay("{Value}")]
	public class TokenString:ITomlToken
	{
		public string Value { get; private set; }
		private static List<Tuple<string, string>> _replacements =
			new List<Tuple<string, string>>
			{
				new Tuple<string, string>("\\", "\\\\"),
				new Tuple<string, string>("\0", "\\0"),
				new Tuple<string, string>("\t", "\\t"),
				new Tuple<string, string>("\n", "\\n"),
				new Tuple<string, string>("\r", "\\r"),
				new Tuple<string, string>("\"", "\\\"")
			};

		public TokenString(IEnumerable<char> enumerable)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in enumerable)
			{
				sb.Append(c);
			}
			Encoding enc = new UTF8Encoding(true, true);
			Value = enc.GetString(enc.GetBytes(sb.ToString()));
		}

		public override string ToString()
		{
			return '"' + Value + '"';
		}

		public string GetOutput()
		{
			if (string.IsNullOrEmpty(Value))
			{
				return "";
			}
			return
				'"' +
				_replacements
					.Aggregate(
					           Value,
					           (current, replacement) =>
					           current.Replace(replacement.Item1, replacement.Item2)
					)
				+ '"';
		}
	}
}