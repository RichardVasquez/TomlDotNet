using System.Collections.Generic;
using System.Linq;

namespace TOML.DocumentElements
{
	public class TomlKeyGroupName
	{
		public string Name { get; private set; }
		public List<string> Hashes
		{
			get {
				return new List<string>(_hashes);
			}
		}
		private readonly List< string> _hashes = new List<string>();

		public TomlKeyGroupName(string first, IEnumerable<string> rest)
		{
			List<string> temp = new List<string>{first};
			temp.AddRange(rest.Where(s => !string.IsNullOrEmpty(s)));
			Name = string.Join(".", temp);
			_hashes = temp;
		}
	}
}