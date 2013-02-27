using System.Diagnostics;
using System.Text;

namespace TOML.DocumentElements
{
	[DebuggerDisplay("{Debug()}")]
	public class TomlKeyValue
	{
		public string Key { get; private set; }
		public ITomlToken Value { get; private set; }

		public TomlKeyValue(string key, ITomlToken val)
		{
			Key = key;
			Value = val;
		}

		private string Debug()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Key).Append(" = ").Append(Value);
			return sb.ToString();
		}
	}
}