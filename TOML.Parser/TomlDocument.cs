using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TOML
{
	public class TomlDocument:DynamicObject
	{
		private TomlAssignments _rootBlock;
		private List<TomlBlock> _namedBlocks;
		private List<TomlHash> _variables = new List<TomlHash>();
		private List<ITomlToken> _arrays = new List<ITomlToken>();

		public TomlDocument(TomlAssignments baseAssignments, IEnumerable<TomlBlock> keyGroups)
		{
			_rootBlock = baseAssignments;
			if (keyGroups != null)
			{
				_namedBlocks = keyGroups.ToList();
			}
			Munge();
		}

		private void Munge()
		{
			MungeRootBlock();
			MungeNamedBlocks();
		}

		private void MungeNamedBlocks()
		{
			foreach (var block in _namedBlocks)
			{
				List<string> baseHashes = block.Group.Hashes;
				foreach (var variable in block.Assignments.Assignments)
				{
					List<string> tempName = new List<string>(baseHashes);
					tempName.Add(variable.Key);
					TomlHash th = new TomlHash
					{
						Data = variable.Value,
						NameParts = tempName
					};

					//	We have a duplicate name.  Time to die.
					if (_variables.FirstOrDefault(v => v.Name == th.Name) != null)
					{
						throw new InvalidDataException(th.Name + " is a duplicate.");
					}
					_variables.Add(th);
				}
			}
		}

		private void MungeRootBlock()
		{
			foreach (var tomlKeyValue in _rootBlock.Assignments)
			{
				TomlHash th = new TomlHash
				{
					Data = tomlKeyValue.Value,
					NameParts = new List<string> {tomlKeyValue.Key}
				};

				if (_variables.FirstOrDefault(v => v.Name == th.Name) != null)
				{
					throw new InvalidDataException(th.Name + " is a duplicate.");
				}
				_variables.Add(th);
			}

		}

		/// <summary>
		/// Constructor for variable (string based hashes) assignments
		/// </summary>
		private TomlDocument(List<TomlHash> baseAssignments)
		{
			_variables = baseAssignments;
		}

		/// <summary>
		/// Constructor for array assignments
		/// </summary>
		private TomlDocument(List<ITomlToken> baseAssignments)
		{
			if (baseAssignments != null && baseAssignments.Count > 0)
			{
				_arrays = baseAssignments;
			}
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			List<TomlHash> lth = _variables.Where(hash => hash.NameParts.First() == binder.Name).ToList();
			switch (lth.Count)
			{
				case 0:
					result = null;
					return false;
				case 1:
					result = UnboxTomlObject(lth[ 0 ].Data);
					return true;
				default:
					foreach (var hash in lth)
					{
						hash.NameParts.RemoveAt(0);
					}
					result = new TomlDocument(lth);
					return true;
			}
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			int k = 0;

			result = 3;
			return true;
		}

		private object UnboxTomlObject(ITomlToken data)
		{
			if (data is TokenString)
			{
				return ( data as TokenString ).Value;
			}

			if (data is TokenInteger)
			{
				return ( data as TokenInteger ).Value;
			}

			if (data is TokenFloat)
			{
				return ( data as TokenFloat ).Value;
			}

			if (data is TokenDateTime)
			{
				return ( data as TokenDateTime ).Value;
			}

			if (data is TokenBool)
			{
				return ( data as TokenBool ).Value;
			}

			if (data is TokenArray)
			{
				return new TomlDocument(( data as TokenArray ).Value);
			}

			throw new ArgumentException("Unusable data type: " + data.GetType());
		}

		//public TomlAssignments RootLevel;
		//public List<TomlBlock> KeyGroups = new List<TomlBlock>();
		//public TomlDocument(TomlAssignments h0, IEnumerable<TomlBlock> nb)
		//{
		//    RootLevel = h0;
		//    KeyGroups = nb.ToList();
		//}
	}

	[DebuggerDisplay("{Name}: {Data}")]
	public class TomlHash
	{
		public string Name
		{
			get { return string.Join(".", NameParts); }
		}
		public List<string> NameParts;
		public ITomlToken Data;
	}
}