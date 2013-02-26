using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TOML
{
	[DebuggerDisplay("{Name}: {Data}")]
	public class TomlHash
	{
		public List<string> NameParts;
		public ITomlToken Data;
		public string Name
		{
			get { return string.Join(".", NameParts); }
		}

		public bool MatchesName(string s)
		{
			var parts = s.Split(new[] {'.'});
			return !parts.Where((t, i) => t != NameParts[ i ]).Any();
		}

		public void TrimName(string s)
		{
			if (!MatchesName(s))
			{
				return;
			}
			var parts = s.Split(new[] {'.'});
			NameParts.RemoveRange(0,parts.Count());
		}

		public List<string> SubNames()
		{
			List<string> temp = new List<string>();
			for (int i = 1; i <= NameParts.Count; i++)
			{
				temp.Add(string.Join(".", NameParts.GetRange(0, i).ToArray()));
			}
			return temp;
		}
	}
}