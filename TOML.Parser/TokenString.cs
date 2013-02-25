using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TOML
{
	[DebuggerDisplay("{Value}")]
	public class TokenString:ITomlToken
	{
		public string Value { get; private set; }

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
	}
}