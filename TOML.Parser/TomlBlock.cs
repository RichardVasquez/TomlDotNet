namespace TOML
{
	public class TomlBlock:ITomlData
	{
		public TomlKeyGroupName Group { get; private set; }
		public TomlAssignments Assignments { get; private set; }

		public TomlBlock(TomlKeyGroupName tomlKeyGroupName, TomlAssignments tomlAssignments)
		{
			Group = tomlKeyGroupName;
			Assignments = tomlAssignments;
		}
	}
}