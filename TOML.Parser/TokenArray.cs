using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TOML
{
	[DebuggerDisplay("{DebugAttribute}")]
	public class TokenArray:ITomlToken
	{
		private bool _prettyPrint = false;
		private const int Spacing = 3;
		private const int MaxWidth = 80;

		public List<ITomlToken> Value;
		public string DebugAttribute
		{
			get { return Debug(); }
		}

		public TokenArray(ITomlToken first, IEnumerable<ITomlToken> rest)
		{
			Value = new List<ITomlToken>();
			Value.Add(first);
			foreach (ITomlToken item in rest)
			{
				Value.Add(item);
			}
		}

		private string Debug()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('[');
			List<string> temp = Value.Select(item => item.ToString()).ToList();
			sb.Append(string.Join(", ", temp));
			sb.Append(']');
			return sb.ToString();
		}

		public int Depth
		{
			get
			{
				int i = 1;
				foreach (int d in 
					Value.OfType<TokenArray>()
					.Select(token => token.Depth + i)
					.Where(d => d > i))
				{
					return d;
				}
				return i;
			}
		}

		public override string ToString()
		{
			if (Value == null)
			{
				return "null";
			}

			if (Value.Count == 0)
			{
				return "empty";
			}

			string s = Value[ 0 ].GetType().ToString();
			return string.Format("Array of {0} with {1} elements.", s, Value.Count);
		}

		public string GetOutput()
		{
			if (!_prettyPrint)
			{
				StringBuilder sb = new StringBuilder();
				List<string> output = Value.Select(tomlToken => tomlToken.GetOutput()).ToList();
				sb.Append("[ ").Append(string.Join(", ", output)).Append(" ]");
				return sb.ToString();
			}

			return GetOutput(0);
		}

		public string GetOutput(int depth)
		{
			string spc = new string(' ', depth * Spacing);
			StringBuilder sb = new StringBuilder();

			List<string> output = Value.Select(tomlToken => tomlToken.GetOutput()).ToList();

			sb.AppendLine("[");
			
			//Append(string.Join(", ", output)).AppendLine().AppendLine("]");

			sb.Append("]");

			return sb.ToString();
		}

	}
}