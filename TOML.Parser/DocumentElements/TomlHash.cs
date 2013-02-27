using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TOML.DocumentElements
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

		public string KeyGroup
		{
			get
			{
				if (NameParts == null || NameParts.Count < 2)
				{
					return "";
				}

				return string.Join(".", NameParts.Take(NameParts.Count - 1).ToList());
			}
		}

		public string Variable
		{
			get { return NameParts.Last(); }
		}

		public string Output
		{
			get { return string.Format("{0} = {1}", Variable, Data.GetOutput()); }
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

		public List<string> SubNames(List<string> ls)
		{
			if (ls == null)
			{
				return null;
			}
			if (ls.Count >= NameParts.Count)
			{
				return null;
			}

			for (int i = 0; i < ls.Count; i++ )
			{
				if (ls[i] != NameParts[i])
				{
					return null;
				}
			}

			return NameParts.GetRange(0, ls.Count+1);
		}
	}
}