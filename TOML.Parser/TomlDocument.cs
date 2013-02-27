using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using TOML.DocumentElements;
using TOML.ParserTokens;

namespace TOML
{
	public class TomlDocument:DynamicObject
	{
		private readonly TomlAssignments _rootBlock;
		private readonly List<TomlBlock> _namedBlocks;
		private readonly List<TomlHash> _variables = new List<TomlHash>();
		private readonly List<ITomlToken> _arrays = new List<ITomlToken>();

		// ReSharper disable UnusedMember.Local
		private TomlDocument()
		{
			throw new NotImplementedException("Only construct from TomlParser.TryParse()");
		}
		// ReSharper restore UnusedMember.Local

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
			foreach (TomlHash th in _rootBlock.Assignments.Select(tomlKeyValue => new TomlHash
			{
				Data = tomlKeyValue.Value,
				NameParts = new List<string> {tomlKeyValue.Key}
			}))
			{
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
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			List<TomlHash> lth = _variables.Where(hash => hash.NameParts.First() == binder.Name).ToList();
			switch (lth.Count)
			{
				case 0:
					result = null;
					return false;
				case 1:
					if (lth[ 0 ].NameParts.Count == 1)
					{
						result = UnboxTomlObject(lth[ 0 ].Data);
						return true;
					}
					TomlHash snip = new TomlHash
					{
						Data = lth[ 0 ].Data,
						NameParts = lth[ 0 ].NameParts.Skip(1).ToList()
					};
					result = new TomlDocument(new List<TomlHash>{snip});
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
		/// TomlDocument is read-only.
		/// </summary>
		/// <remarks>
		/// In short, no.  In long, NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO.
		/// </remarks>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			return false;
		}

		/// <summary>
		/// TomlDocument is read-only.
		/// </summary>
		/// <remarks>
		/// In short, no.  In long, NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO.
		/// </remarks>
		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			return false;
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

		/// <summary>
		/// Simple debug code that shows the stored contents.
		/// </summary>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			List<string> members
				= _variables.Count > 0
					  ? GetVariableMembers()
					  : GetArrayMembers();

			return members;
		}

		/// <summary>
		/// You really shouldn't have this type of situation,
		/// but if you're grabbing sections of the tree structure,
		/// here's the remaining arrays.
		/// </summary>
		private List<string> GetArrayMembers()
		{
			return _arrays.OfType<TokenArray>()
				.Select(
					v =>
						(
							from item in v.Value
								let cel = item as TokenArray
			                    let t = ( UnboxTomlObject(item).ToString() )
			                    select cel != null
				                ? string.Format("[{0}]", cel.Depth)
				                : string.Format("[{0}]", t)
						)
						.Aggregate(
							"",
							(current, output) => current + output)
						).ToList();
		}

		/// <summary>
		/// Provides a list of hash names available via the varying indexing/naming retrieval values.
		/// </summary>
		private List<string> GetVariableMembers()
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

		/// <summary>
		/// Presents the text needed to recreate the TOML file, minus the initial comments.
		/// </summary>
		/// <remarks>
		/// It also does a strong cleanup of the data, alphabetizing the keygroups and all
		/// the keys within each group.  It also puts the arrays at the end of each group,
		/// with some pretty printing for wide values.
		/// </remarks>
		public override string ToString()
		{
			if (_variables == null || _variables.Count == 0)
			{
				return ShowEmpty();
			}

			List<string> sets = _variables.Select(hash => hash.KeyGroup).Distinct().ToList();
			sets.Sort();

			StringBuilder sb = new StringBuilder();
			foreach (List<TomlHash> lth in sets.Select(s1 => _variables.Where(v => v.KeyGroup == s1).ToList()))
			{
				var v = lth.FirstOrDefault();
				if (v == null)
				{
					continue;
				}

				//	output sorts the keys and puts arrays at the end.  It's an aesthetic thing, is all.
				if (v.KeyGroup != "")
				{
					sb.AppendFormat("[{0}]", v.KeyGroup).AppendLine();
				}
				foreach (TomlHash tomlHash in lth.Where(thType => !(thType.Data is TokenArray)).OrderBy(th => th.Variable))
				{
					sb.AppendLine(tomlHash.Output);
				}
				foreach (TomlHash tomlHash in lth.Where(thType => thType.Data is TokenArray).OrderBy(th => th.Variable))
				{
					sb.AppendLine(tomlHash.Output);
				}
				sb.AppendLine();
			}

			return sb.ToString();
		}

		/// <summary>
		/// In case you parsed nothing or created a brand new TomlDocument from scratch.
		/// </summary>
		/// <remarks>
		/// Since TomlDocument is effectively read-only, you shouldn't be creating one from scratch.
		/// </remarks>
		private static string ShowEmpty()
		{
			return "# Empty TOML";
		}
	}
}