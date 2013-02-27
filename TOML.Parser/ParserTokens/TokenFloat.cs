using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sprache;

namespace TOML.ParserTokens
{
	[DebuggerDisplay("{Value}")]
	public class TokenFloat:ITomlToken
	{
		public double Value { get; private set; }

		public TokenFloat(IOption<IEnumerable<char>> minus, string number1, string number2)
		{
			StringBuilder sb = new StringBuilder();
			if (minus.IsDefined && !minus.IsEmpty)
			{
				foreach (char c in minus.Get())
				{
					sb.Append(c);
				}
			}

			sb.Append(number1).Append('.').Append(number2);

			double f;
			if (!Double.TryParse(sb.ToString(), out f))
			{
				throw new ArgumentException(string.Format("Bad float format: {0}", sb));
			}
			Value = f;
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