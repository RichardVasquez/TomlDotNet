using System;
using System.Collections;
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

		/// <summary>
		/// Get all the key/group names
		/// </summary>
		private void Munge()
		{
			MungeRootBlock();
			MungeNamedBlocks();
		}

		/// <summary>
		/// Get the sub keygroups
		/// </summary>
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

		/// <summary>
		/// Get the keys that don't belong to any named group.
		/// </summary>
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

		/// <summary>
		/// The type of data you're trying to retrieve (either a keygroup or an actual key)
		/// will determine if you receive a "child" TomlDocument or an actual value.
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="result"></param>
		/// <returns></returns>
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
					List<TomlHash> fin =
						lth.Select(
						           hash =>
						           new TomlHash {Data = hash.Data, NameParts = new List<string>(hash.NameParts)})
						   .ToList();

					foreach (var hash in fin)
					{
						hash.NameParts.RemoveAt(0);
					}
					result = new TomlDocument(fin);
					return true;
			}
		}

		/// <summary>
		/// Process indexers for TomlDocument handling both [x,y,z] and [x][y][z] indexing.
		/// </summary>
		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			object[] o = {new object()};
			if (indexes.Count() == 1)
			{
				ProcessSingleIndex(indexes[ 0 ], out o[ 0 ]);
			}
			else
			{
				//	For multiple indexers such as [ "clients", "data", 0, 1 ], the indexers are always going
				//	to be string first, then integers since as of this moment, you can't have something like:
				//	[clients] data = [ [words1] ["gamma","delta"], [data2] [1, 2] ]
				//
				//	Note, at which point, if something like the above were allowed, would lend to screams heard
				//	round the world.
				o[ 0 ] = _arrays.Count != 0
					    ? new TomlDocument(_arrays)
					    : new TomlDocument(_variables);
				foreach (var index in indexes.Where(index => o[ 0 ] != null && index !=null))
				{
					o[ 0 ] = (o[ 0 ] as dynamic)[ index ];
				}
			}
			result = o[ 0 ];
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
					List<TomlHash> fin =
						vars.Select(
						            hash =>
						            new TomlHash {Data = hash.Data, NameParts = new List<string>(hash.NameParts)})
						    .ToList();
					foreach (var hash in fin)
					{
						hash.TrimName(idx);
					}

					var cleared = fin.Where(v => v.Name == "").ToList();
					switch (cleared.Count)
					{
						case 0:
							result = new TomlDocument(fin);
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
		/// Unboxes to a primitive or a TomlDocument.
		/// </summary>
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

		/// <summary>
		/// Returns a Hashset where all keygroups are broken down by key names with
		/// numeric indices for arrays.  Using the original TOML document provided,
		/// You would acess the value "alpha" via ["clients"]["hosts"][0].
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public Hashtable GetTreeHash(string root = "")
		{
			Hashtable ht = new Hashtable();
			var names = GetAllHashNames(root);
			if (names.Count > 0)
			{
				foreach (var name in names)
				{
					var v = _variables.FirstOrDefault(vn => vn.Name == name);
					string keyName = name.Substring(root.Length);
					if (keyName.StartsWith("."))
					{
						keyName = keyName.Substring(1);
					}
					if (v != null)
					{
						//	really here
						if (!(v.Data is TokenArray))
						{
							ht[ keyName ] = UnboxTomlObject(v.Data);
						}
						else
						{
							ht[ keyName ] = MakeNestedHash(v.Data);
						}
					}
					else
					{
						//	Go down the tree
						ht[keyName] = GetTreeHash(name);
					}
				}
			}
			return ht;
		}

		/// <summary>
		/// Finds the hash names that are immediate children of filter.
		/// </summary>
		private List<string> GetAllHashNames(string filter)
		{
			var parts = filter.Split(new[] {'.'},StringSplitOptions.RemoveEmptyEntries).ToList();
			HashSet<string> temp = new HashSet<string>();
			foreach (TomlHash v in _variables)
			{
				if (parts.Count == 0)
				{
					temp.Add(v.NameParts.First());
				}
				else
				{
					var sn = v.SubNames(parts);
					if (sn != null)
					{
						temp.Add(string.Join(".", sn));
					}
				}
			}
			List<string> res = temp.ToList();
			res.Sort();
			return res;
		}

		/// <summary>
		/// Returns a flattened Hashset where all the text keys are defined as
		/// full hash names.  Arrays have numerical indices.  Using the original
		/// TOML document provided, you would acess the value "alpha" via
		/// ["clients.hosts"][0].
		/// </summary>
		public Hashtable GetFlatHash()
		{
			Hashtable ht = new Hashtable();
			IEnumerable<string> empty = BuildEmptyHashes();

			foreach (string s in empty)
			{
				ht[ s ] = new Hashtable();
			}

			if (_arrays.Count > 0)
			{
				int idx = 0;
				foreach (ITomlToken tomlToken in _arrays)
				{
					if (!( tomlToken is TokenArray ))
					{
						ht[ idx++ ] = UnboxTomlObject(tomlToken);
					}
					else
					{
						ht[ idx++ ] = MakeNestedHash(tomlToken);
					}
				}
			}
			else
			{
				foreach (TomlHash hash in _variables)
				{
					if (!( hash.Data is TokenArray ))
					{
						ht[ hash.Name ] = UnboxTomlObject(hash.Data);
					}
					else
					{
						ht[ hash.Name ] = MakeNestedHash(hash.Data);
					}
				}
			}

			return ht;

		}

		/// <summary>
		/// Unboxes a data element within an array or recurses to further process an array.
		/// </summary>
		private static object MakeNestedHash(ITomlToken tomlToken)
		{
			Hashtable nht = new Hashtable();
			var a = tomlToken as TokenArray;
			if (a == null)
			{
				return nht;
			}
			for (int i = 0; i < a.Value.Count; i++)
			{
				if (a.Value[ i ] is TokenArray)
				{
					nht[ i ] = MakeNestedHash(a.Value[ i ]);
				}
				else
				{
					nht[ i ] = UnboxTomlObject(a.Value[ i ]);
				}
			}
			return nht;
		}

		/// <summary>
		/// Finds empty hash keys to ensure they are retrievable even if not defined
		/// </summary>
		private IEnumerable<string> BuildEmptyHashes()
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
			return tempNulls.ToList();
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			List<string> members = new List<string>();

			foreach (var va in _variables)
			{
				string s = va.Name;
				if (va.Data is TokenArray)
				{
					int d = ( va.Data as TokenArray ).Depth;
					for (int i = 0; i < d; i++)
					{
						s += "[]";
					}
				}
				members.Add(s);
			}
			return members;
		}


	}
}