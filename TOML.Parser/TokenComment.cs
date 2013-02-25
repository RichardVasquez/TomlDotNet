using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TOML
{
	[DebuggerDisplay("{Value}")]
	public class TokenComment:ITomlToken
	{
		public new string Value { get; private set; }

		public TokenComment(char hash, IEnumerable<char> comment)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char c in comment)
			{
				sb.Append(c);
			}
			Value = sb.ToString();
		}

		public override string ToString()
		{
			return "#" + Value;
		}
	}
}