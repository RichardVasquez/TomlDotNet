using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TOML.ParserTokens
{
	[DebuggerDisplay("{DebugAttribute}")]
	public class TokenArray:ITomlToken
	{
		private bool _prettyPrint = true;
		private const int Spacing = 3;
		private const int MaxWidth = 50;

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
			if (Value == null || Value.Count == 0)
			{
				return "[] # Empty Array";
			}

			if (!_prettyPrint)
			{
				StringBuilder sb = new StringBuilder();
				List<string> output = Value.Select(tomlToken => tomlToken.GetOutput()).ToList();
				sb.Append("[ ").Append(string.Join(", ", output)).Append(" ]");
				return sb.ToString();
			}

			List<PrettyPrint> tokens = GetPrettyPrintTokens();
			PrettyArray pa = new PrettyArray(tokens);
			string s = MakePrettyPrint(pa);
			return s;
		}

		private string MakePrettyPrint(PrettyArray array, int depth = 0)
		{
			string spcBracket = new string(' ', depth * Spacing);
			string spcPadding = new string(' ', (depth + 1) * Spacing);
			int currentSpace = MaxWidth - Spacing * depth;
			if (array.Output.Length <= currentSpace)
			{
				return spcBracket + array.Output;
			}
			
			StringBuilder sb = new StringBuilder();
			if (depth == 0)
			{
				sb.AppendLine("[");
				//sb.AppendLine().Append("]");
			}
			else
			{
				sb.AppendLine().Append(spcBracket).AppendLine("[");
				//sb.AppendLine().Append(spcBracket).Append("]");
			}

			foreach (var prettyPrint in array.Tokens)
			{
				if (prettyPrint is PrettyArray)
				{
					sb.Append(MakePrettyPrint(prettyPrint as PrettyArray, depth + 1));
					continue;
				}
				if (prettyPrint is PrettyText)
				{
					sb.Append(spcPadding).Append(prettyPrint.Output);
					continue;
				}
				if (prettyPrint is PrettyComma)
				{
					sb.AppendLine(prettyPrint.Output);
				}
			}
			if (depth == 0)
			{
				sb.AppendLine().Append("]");
			}
			else
			{
				sb.AppendLine().Append(spcBracket).Append("]");
			}

			return sb.ToString();
		}

		public List<PrettyPrint> GetPrettyPrintTokens()
		{
			List<PrettyPrint> output = new List<PrettyPrint> {new PrettyBracketOpen(), new PrettySpace()};

			foreach (ITomlToken token in Value)
			{
				if (token is TokenArray)
				{
					output.Add(new PrettyArray((token as TokenArray).GetPrettyPrintTokens()));
				}
				else
				{
					output.Add(new PrettyText(token.GetOutput()));
				}
				if (token != Value.Last())
				{
					output.Add(new PrettyComma());
				}
				output.Add(new PrettySpace());
			}
			output.Add(new PrettyBracketClose());
			return output;
		}

		private void GetArrayOutput(int depth)
		{
			
		}

		public abstract class PrettyPrint
		{
			public virtual int Length {
				get { return Output.Length; }
			}

			public virtual string Output { get; protected set; }
			internal PrettyPrint(string s)
			{
				Output = s;
			}
		}

		internal class PrettySpace:PrettyPrint
		{
			 internal PrettySpace():base(" ")
			 {
			 }
		}

		internal class PrettyBracketOpen:PrettyPrint
		{
			internal PrettyBracketOpen():base("[")
			{
			}
		}

		internal class PrettyBracketClose : PrettyPrint
		{
			internal PrettyBracketClose()
				: base("]")
			{
			}
		}

		internal class PrettyText : PrettyPrint
		{
			internal PrettyText(string s) : base(s)
			{
			}
		}

		internal class PrettyComma:PrettyPrint
		{
			internal PrettyComma():base(",")
			{
			}
		}

		public class PrettyArray:PrettyPrint
		{
			private readonly List<PrettyPrint> _items = new List<PrettyPrint>();

			public List<PrettyPrint> Tokens { get { return _items; } }

			public override int Length
			{
				get { return Output.Length; }
			}

			public override string Output
			{
				get
				{
					StringBuilder sb = new StringBuilder();
					foreach (var i in _items)
					{
						sb.Append(i.Output);
					}
					return sb.ToString();
				}
			}

			public string GetOutput(int space, int width)
			{
				int availble = width - space;
				StringBuilder sb = new StringBuilder();
				sb.Append("[ ");



				sb.Append(" ]");
				return sb.ToString();
			}


			public PrettyArray(List<PrettyPrint> items):base("")
			{
				_items = items;
			}
		}

		internal class PrettyEol:PrettyPrint
		{
			internal PrettyEol() : base(Environment.NewLine)
			{
			}
		}

	}
}