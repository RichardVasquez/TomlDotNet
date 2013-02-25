using System.Diagnostics;

namespace TOML
{
	[DebuggerDisplay("{Value}")]
	public class TokenBool:ITomlToken
	{
		public bool Value { get; private set; }
		public TokenBool(bool b)
		{
			Value = b;
		}
		public override string ToString()
		{
			return Value.ToString();
		}
	}
}