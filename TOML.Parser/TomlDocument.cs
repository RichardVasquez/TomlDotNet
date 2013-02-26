using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace TOML
{
	public class TomlDocument:DynamicObject
	{
		private readonly TomlAssignments _rootBlock;
		private readonly List<TomlBlock> _namedBlocks;
		private readonly List<TomlHash> _variables = new List<TomlHash>();
		private readonly List<ITomlToken> _arrays = new List<ITomlToken>();

		#region Base constructor
		/// <summary>
		/// Initial class resulting from the parsing of a Toml document.
		/// </summary>
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
			//BuildEmptyHashes();
		}

		private void BuildEmptyHashes()
		{
			HashSet<string> tempNulls = new HashSet<string>();
			foreach (string name in _variables.SelectMany(hash => hash.SubNames()))
			{
				tempNulls.Add(name);
			}
			foreach (TomlHash hash in _variables)
			{
				tempNulls.Remove(hash.Name);
			}
			foreach (var p in tempNulls.Select(tempNull => tempNull.Split(new[] {'.'}).ToList()))
			{
				_variables.Add(new TomlHash{Data = new TokenNull(), NameParts = p});
			}
		}

		private void MungeNamedBlocks()
		{
			foreach (var block in _namedBlocks)
			{
				List<string> baseHashes = block.Group.Hashes;
				foreach (TomlHash th in from variable in block.Assignments.Assignments
				                        let tempName = new List<string>(baseHashes) {variable.Key}
				                        select new TomlHash
				                        {
					                        Data = variable.Value,
					                        NameParts = tempName
				                        })
				{
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
		#endregion

		#region Recursive constructor 
		/// <summary>
		/// Constructor for variable (string based hashes) assignments
		/// </summary>
		private TomlDocument(List<TomlHash> baseAssignments)
		{
			_variables = baseAssignments;
		}
		#endregion

		/// <summary>
		/// Constructor for array assignments.  At this point, we should be out of
		/// hashes, so no need to worry about the other possibilities.
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

		/// <summary>
		/// Process indexers for TomlDocument handling both [x,y,z] and [x][y][z] indexing.
		/// </summary>
		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			object o = new object();
			if (indexes.Count() == 1)
			{
				ProcessSingleIndex(indexes[ 0 ], out o);
			}
			else
			{
				//	For multiple indexers such as [ "clients", "data", 0, 1 ], the indexers are always going
				//	to be string first, then integers since as of this moment, you can't have something like:
				//	[clients] data = [ [words1] ["gamma","delta"], [data2] [1, 2] ]
				//
				//	Note, at which point, if something like the above were allowed, would lend to screams heard
				//	round the world.
				o = _arrays.Count != 0
					    ? new TomlDocument(_arrays)
					    : new TomlDocument(_variables);
				foreach (var index in indexes.Where(index => o != null && index !=null))
				{
					o = (o as dynamic)[ index ];
				}
			}
			result = o;
			return true;
		}

		/// <summary>
		/// Unfolds a TomlDocument, recursing as needed, and unboxes a value if it's available.
		/// </summary>
		private void ProcessSingleIndex(object index, out object result)
		{
			if (index is string)
			{
				string idx = index as string;
				List<TomlHash> vars = _variables.Where(v => v.MatchesName(idx)).ToList();
				if (vars.Count > 0)
				{
					foreach (var hash in vars)
					{
						hash.TrimName(idx);
					}

					var cleared = vars.Where(v => v.Name == "").ToList();
					switch (cleared.Count)
					{
						case 0:
							result = new TomlDocument(vars);
							break;
						case 1:
							result = UnboxTomlObject(cleared[ 0 ].Data);
							break;
						default:
							throw new KeyNotFoundException();
					}
					return;
				}
				throw new IndexOutOfRangeException();
			}

			if (index is int)
			{
				int idx = (int) index;
				if (idx < 0 || idx >= _arrays.Count)
				{
					throw new IndexOutOfRangeException();
				}

				result = UnboxTomlObject(_arrays[ idx ]);
				return;
			}

			throw new ArgumentException("Invalid key type: Only strings or ints allowed.");
		}

		/// <summary>
		/// Unboxes to a primitive or an TomlDocument.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static object UnboxTomlObject(ITomlToken data)
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


	}
}