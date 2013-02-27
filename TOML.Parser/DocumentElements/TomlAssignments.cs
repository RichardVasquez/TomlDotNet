using System.Collections.Generic;

namespace TOML.DocumentElements
{
	public class TomlAssignments
	{
		public List<TomlKeyValue> Assignments = new List<TomlKeyValue>();
		public TomlAssignments(IEnumerable<TomlKeyValue> block)
		{
			foreach (TomlKeyValue tomlKeyValue in block)
			{
				Assignments.Add(tomlKeyValue);
			}
		}
	}
}