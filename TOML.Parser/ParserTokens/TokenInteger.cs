using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sprache;

namespace TOML.ParserTokens
{
	[DebuggerDisplay("{Value}")]
	public class TokenInteger : ITomlToken
	{
		public Int64 Value { get; private set; }

		internal TokenInteger(IOption<IEnumerable<char>> minus, string number)
		{
			StringBuilder sb = new StringBuilder();
			if (minus.IsDefined && !minus.IsEmpty)
			{
				foreach (char c in minus.Get())
				{
					sb.Append(c);
				}
			}

			sb.Append(number);

			Int64 v;
			if (!Int64.TryParse(sb.ToString(), out v))
			{
				throw new ArgumentException(string.Format("Bad Int64 format: {0}", sb));
			}
			Value = v;
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		public string GetOutput()
		{
			return Value.ToString();
		}
	}
}