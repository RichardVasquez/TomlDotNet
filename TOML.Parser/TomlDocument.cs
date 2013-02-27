using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace TOML
{
	public class TomlDocument:DynamicObject
	{
		private const int Spacing = 5;
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
			List<string> members;
			members = _variables.Count > 0
				          ? GetVariableMembers()
				          : GetArrayMembers();

			return members;
		}

		private List<string> GetArrayMembers()
		{
			List<string> members = new List<string>();
			foreach (var token in _arrays)
			{
				var v = token as TokenArray;
				if (v == null)
				{
					continue;
				}
				string fin ="";
				foreach (var item in v.Value)
				{
					string output;
					var cel = item as TokenArray;
					string t = ( UnboxTomlObject(item).ToString() );
					output = cel != null
						         ? string.Format("[{0}]", cel.Depth)
						         : string.Format("[{0}]", t);
					fin += output;
				}
				members.Add(fin);
			}
			return members;
		}

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

		public override string ToString()
		{
			Hashtable ht = GetTreeHash();

			return TextTree(ht);
		}

		private string TextTree(Hashtable ht, string name = "", int depth = 0)
		{
			StringBuilder sb = new StringBuilder();
			if (ht == null)
			{
				return "";
			}

	
			if (depth > 0 && name != "")
			{
				sb.AppendLine();
				sb.Append(Spacer(depth));
				sb.Append("[").Append(name).AppendLine("]");
			}

			foreach (DictionaryEntry entry in ht)
			{
				if (entry.Key is string)
				{
					if (!( entry.Value is Hashtable ))
					{
						sb.Append(MakeToStringLine(entry, depth));
					}
					else
					{
						bool allInts = IsArray(entry);
						//	we have an array
						if (allInts)
						{
							sb.Append(entry.Key).Append(" = ");
							sb.Append(NewArray(entry.Value as Hashtable, depth));
							sb.AppendLine();
						}
						else
						{
							//	we have additional keys
							string groupName = name;
							if (groupName != "")
							{
								groupName += ".";
							}
							sb.Append(TextTree(entry.Value as Hashtable, groupName + entry.Key, depth + 1));
						}

					}
				}
			}
			return sb.ToString();
		}

		private string NewArray(Hashtable entry, int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			if (entry.Count == 0)
			{
				sb.Append(" ] # Empty array?").AppendLine();
				return sb.ToString();
			}

			List<string> output = new List<string>();
			List<object> newKeys = entry.Keys.Cast<object>().ToList();
			List<int> intKeys = newKeys.OfType<int>().ToList();
			intKeys.Sort();

			foreach (int intKey in intKeys)
			{
				foreach (object newKey in newKeys)
				{
					if (!( newKey is int ))
					{
						continue;
					}
					if ((int) newKey != intKey)
					{
						continue;
					}
					if (!entry.ContainsKey(intKey))
					{
						continue;
					}

					if (!( entry[ intKey ] is Hashtable ))
					{
						output.Add(CreateString(( entry[ intKey ] )));
					}
					else
					{
						StringBuilder sb2 = new StringBuilder();
						sb2.AppendLine().Append(Spacer(depth + 1))
						   .Append(
						           NewArray(entry[ intKey ] as Hashtable, depth + 1));
						output.Add(sb2.ToString());
					}
				}
			}


			//foreach (DictionaryEntry de in entry)
			//{
			//    if (de.Value is Hashtable)
			//    {
			//        StringBuilder sb2 = new StringBuilder();
			//        sb2.AppendLine().Append(Spacer(depth+1))
			//          .Append(NewArray(de.Value as Hashtable, depth + 1));
			//        output.Add(sb2.ToString());
			//    }
			//    else
			//    {
			//        output.Add(CreateString(de.Value));
			//    }
			//}

			if (output.Count > 0)
			{
				sb.Append(string.Join(", ", output));
			}

			sb.Append(" ]");
			return sb.ToString();
		}

		private static bool IsArray(DictionaryEntry de)
		{
			var ht = de.Value as Hashtable;
			return ht != null && ht.Keys.Cast<object>().All(key => key is int);
		}


		private static string Spacer(int depth)
		{
			return depth > 1
					  ? new string(' ', (depth - 1) * Spacing)
					  : "";
		}

		private string ToStringArray(DictionaryEntry node, int depth)
		{
			//	node.Value has been tested to be an array HashTable since all of its
			//	keys are integers.
			//	

			StringBuilder sb = new StringBuilder("[");
			
			if (IsArray(node))
			{
				var tht = node.Value as Hashtable;
				if (tht == null)
				{
					return "";
				}
				var keys = tht.Keys;
				object[] os = new object[keys.Count];
				keys.CopyTo(os, 0);

				if (os.Length == 0)
				{
					sb.Append(" ] # Empty Array?");
					return sb.ToString();
				}

				List<string> output = new List<string>();
				object check = tht[ os[ 0 ] ];
				bool isString = check is string;

				foreach (var key in os)
				{
					var de = tht[ key ];
					if(de is Hashtable)
					{
						//	Start over
						foreach (object o in de as Hashtable)
						{
							int k = 0;
						}

					}
					else
					{
						//	run through the array.
						output.Add(CreateString(de));
					}
				}

				if (isString)
				{
					if (output.Count > 1)
					{
						for (int i = 0; i < output.Count - 1; i++)
						{
							sb
								.Append(Spacer(depth + 1))
								.Append(output[ i ])
								.AppendLine(",");
						}
					}
					sb
						.Append(Spacer(depth + 1))
						.AppendLine(output.Last());
				}
				else
				{
					sb.Append(Spacer(depth + 1));
					if (output.Count > 1)
					{
						for (int i = 0; i < output.Count - 1; i++)
						{
							sb.Append(output[ i ]).Append(", ");
						}
					}
					sb.AppendLine(output.Last());
				}
			}
			

			sb.Append(Spacer(depth)).AppendLine("]").AppendLine();
			return sb.ToString();
		}

		private string MakeToStringLine(DictionaryEntry entry, int depth)
		{
			StringBuilder sb = new StringBuilder(Spacer(depth));

			object n = entry.Key as string;
			sb.AppendFormat("{0} = ", n);
			object o = entry.Value;

			sb.AppendLine(CreateString(o));
			return sb.ToString();
		}

		private string CreateString(object o)
		{
			if ((o is Int64) || o is double)
			{
				return ToStringNumber(o);
			}

			if (o is bool)
			{
				return ToStringBool(o);
			}

			if (o is DateTime)
			{
				return ToStringDateTime(o);
			}

			if (o is string)
			{
				return @"""" + ToStringString(o) +@"""";
			}

			return ToStringError(o);
		}

		private string ToStringNumber(object o)
		{
			return o.ToString();
		}

		private string ToStringBool(object o)
		{
			bool b = (bool) o;
			return b
				       ? "true"
				       : "false";
		}

		private string ToStringDateTime(object o)
		{
			DateTime d = (DateTime) o;
			return d.ToString("yyyy-MM-ddTHH:mm:ssZ");
		}

		private string ToStringString(object o)
		{
			string s = o as string;
			s = s.Replace("\\", "\\\\");
			s = s.Replace("\0", "\\0");
			s = s.Replace("\t", "\\t");
			s = s.Replace("\n", "\\n");
			s = s.Replace("\r", "\\r");
			s = s.Replace("\"", "\\");
			return s;
		}

		private string ToStringError(object o)
		{
			try
			{
				var entry = (DictionaryEntry) o;
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("null # Error!");
				sb.AppendLine().AppendLine();
				sb.AppendFormat("# Key:   {0}", entry.Key).AppendLine();
				sb.AppendFormat("# Value: {0} ({1})", entry.Value, entry.Value.GetType().FullName).AppendLine();
				sb.AppendLine();
				return sb.ToString();
			}
			catch
			{
				return "# Unable to parse o.";
			}

		}
	}
}