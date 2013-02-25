using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using Sprache;

namespace TOML
{
	public static class Parser
	{
		private static readonly Parser<char> SpecialNull =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('0')
			select '\0';

		private static readonly Parser<char> SpecialTab =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('t')
			select '\t';

		private static readonly Parser<char> SpecialNewLine =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('n')
			select '\n';

		private static readonly Parser<char> SpecialCarriageReturn =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('r')
			select '\r';

		private static readonly Parser<char> SpecialQuote =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('"')
			select '"';

		private static readonly Parser<char> SpecialBackslash =
			from c1 in Parse.Char('\\')
			from c2 in Parse.Char('\\')
			select '\\';

		private static readonly Parser<char> SpecialCharacter =
			SpecialNull
				.Or(SpecialTab)
				.Or(SpecialNewLine)
				.Or(SpecialCarriageReturn)
				.Or(SpecialQuote)
				.Or(SpecialBackslash);

		private static readonly Parser<string> Eol =
			from c1 in Parse.Char('\r').Optional()
			from c2 in Parse.Char('\n')
			select Environment.NewLine;

		public static readonly Parser<TokenComment> Comment =
			from spc1 in Parse.WhiteSpace.Many()
			from hash in Parse.Char('#')
			from comment in Parse.AnyChar.Except(Eol).Many()
			from spc2 in Parse.WhiteSpace.Many()
			select new TokenComment(hash, comment);

		//	Deal with multiple comments.
		private static readonly Parser<char> HandleComments =
			from s in Parse.WhiteSpace.Many()
			from c in Comment.Many()
			select '\0';

		//	Chomp the left opening of an array, ignoring extraneous text
		private static readonly Parser<char> ArrayChompLeft =
			from hc0 in HandleComments
			from lb in Parse.Char('[')
			from hc1 in HandleComments
			select '\0';

		//	Chomp the right opening of an array, ignoring extraneous text
		private static readonly Parser<char> ArrayChompRight =
			from hc0 in HandleComments
			from rb in Parse.Char(']')
			from hc1 in HandleComments
			select '\0';

		public static readonly Parser<TokenInteger> Integer =
			from spc1 in Parse.WhiteSpace.Many()
			from minus in Parse.String("-").Optional()
			from number in Parse.Number.Token()
			from hc in HandleComments
			select new TokenInteger(minus, number);

		public static readonly Parser<TokenFloat> Float =
			from spc1 in Parse.WhiteSpace.Many()
			from minus in Parse.String("-").Optional()
			from number1 in Parse.Number.Token()
			from dot in Parse.Char('.')
			from number2 in Parse.Number.Token()
			from hc in HandleComments
			select new TokenFloat(minus, number1, number2);

		public static readonly Parser<TokenDateTime> Datetime =
			from spc1 in Parse.WhiteSpace.Many()
			from year in Parse.Number.Token()
			from dash1 in Parse.Char('-')
			from month in Parse.Number.Token()
			from dash2 in Parse.Char('-')
			from day in Parse.Number.Token()
			from t in Parse.Char('T')
			from hour in Parse.Number.Token()
			from c1 in Parse.Char(':')
			from minute in Parse.Number.Token()
			from c2 in Parse.Char(':')
			from second in Parse.Number.Token()
			from z in Parse.Char('Z')
			from hc in HandleComments
			select new TokenDateTime(new[] { year, month, day, hour, minute, second });

		public static readonly Parser<TokenString> String =
			from spc1 in Parse.WhiteSpace.Many()
			from q1 in Parse.Char('"')
			from c in
				(
					from c1 in SpecialCharacter.Or(Parse.CharExcept('"')).Many()
					select c1
				)
			from q2 in Parse.Char('"')
			from hc in HandleComments
			select new TokenString(c);

		public static readonly Parser<TokenBool> Boolean =
			from spc1 in Parse.WhiteSpace.Many()
			from b in
				( from b1 in Parse.String("true").Token()
				  select true
				).Or(
				     from b2 in Parse.String("false").Token()
				     select false
				)
			from hc in HandleComments
			select new TokenBool(b);

		private static readonly Parser<char> Comma = Parse.Char(',');

		private static readonly Parser<TokenArray> ListInteger =
			from first in Integer
			from rest in
				Comma
				.Then(_ => Comment.Many())
				.Then(_ => Integer).Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		private static readonly Parser<TokenArray> ListFloat =
			from first in Float
			from rest in
				Comma
				.Then(_ => Comment.Many())
				.Then(_ => Float)
				.Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		private static readonly Parser<TokenArray> ListString =
			from first in String
			from rest in
				Comma
				.Then(_ => Comment.Many())
				.Then(_ => String)
				.Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		private static readonly Parser<TokenArray> ListBoolean =
			from first in Boolean
			from rest in
				Comma
				.Then(_ => Comment.Many())
				.Then(_ => Boolean)
				.Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		private static readonly Parser<TokenArray> ListDateTime =
			from first in Datetime
			from rest in
				Comma
				.Then(_ => Comment.Many())
				.Then(_ => Datetime)
				.Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		public static readonly Parser<TokenArray> ArrayInteger =
			from l in ArrayChompLeft
			from list in ListInteger
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> ArrayFloat =
			from l in ArrayChompLeft
			from list in ListFloat
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> ArrayString =
			from l in ArrayChompLeft
			from list in ListString
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> ArrayArray =
			from l in ArrayChompLeft
			from list in ListArray
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> ArrayBoolean =
			from l in ArrayChompLeft
			from list in ListBoolean
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> ArrayDateTime =
			from l in ArrayChompLeft
			from list in ListDateTime
			from r in ArrayChompRight
			select list;

		public static readonly Parser<TokenArray> Array =
			from spc1 in Parse.WhiteSpace.Many()
			from c0 in Comment.Many()
			from array in
				ArrayDateTime
				.Or(ArrayFloat)
				.Or(ArrayInteger)
				.Or(ArrayString)
				.Or(ArrayBoolean)
				.Or(ArrayArray)
			from hc in HandleComments
			select array;

		private static readonly Parser<TokenArray> ListArray =
			from first in Array
			from rest in
				Comma
				.Then(_ => Array)
				.Many()
			from extra in Comma.Optional()
			from hc in HandleComments
			select new TokenArray(first, rest);

		//	Seriously?  All non whitespace?  Feh.  Ok.  Here we go.
		//
		//	I'm also going to block control characters before space.
		//
		//	Also, after some pondering over "Dot is reserved. OBEY.",
		//	Unless it's a nested hash, the dot is not allowed either.
		//
		//	From 2013-02-25:
		//		Keys start with the first non-whitespace character
		//		and end with the last non-whitespace character before
		//		the equals sign.
		public static readonly Parser<string> KeyName =
			from c in
				Parse
				.Char(
				      ch => !Char.IsWhiteSpace(ch)
						  && ch > ' ' && ch != '=' && ch != '.'
						  && ch != '[' && ch != ']'
						  ,
				      "Non whitespace and non control and non equal."
				)
				.AtLeastOnce().Text()
			select c;

		public static readonly Parser<ITomlToken> KeyValue =
			from s0 in Parse.WhiteSpace.Many()
			from v in Datetime
				.Or<ITomlToken>(Float)
				.Or<ITomlToken>(Integer)
				.Or<ITomlToken>(String)
				.Or<ITomlToken>(Boolean)
				.Or<ITomlToken>(Array)
			from s1 in Parse.WhiteSpace.Many()
			select v;


		public static readonly Parser<TomlKeyValue> Assignment =
			from s0 in Parse.WhiteSpace.Many()
			from key in KeyName
			from s1 in Parse.WhiteSpace.Many()
			from eq in Parse.Char('=')
			from s2 in Parse.WhiteSpace.Many()
			from val in KeyValue
			from s3 in Parse.WhiteSpace.Many()
			select new TomlKeyValue(key, val);

		public static readonly Parser<string> IgnoreText =
			from text in
				(
					from hc0 in HandleComments.Many()
					from s0 in Parse.WhiteSpace.Many()
					select string.Empty
				).Many()
			select "";

		public static readonly Parser<TomlAssignments> HashAssignments =
			from block in
				(
					from t0 in IgnoreText
					from a in Assignment
					from t1 in IgnoreText
					select a
				).Many()
			select new TomlAssignments(block);

		public static readonly Parser<TomlKeyGroupName> GroupName =
			from s0 in Parse.WhiteSpace.Many()
			from lb in Parse.Char('[')
			from s1 in Parse.WhiteSpace.Many()
			from first in KeyName
			from rest in
				Parse.Char('.')
				     .Then(_ => KeyName)
				     .Many()
			from s2 in Parse.WhiteSpace.Many()
			from rb in Parse.Char(']')
			from s3 in Parse.WhiteSpace.Many()
			select new TomlKeyGroupName(first, rest);

		public static readonly Parser<ITomlData> NamedBlock =
			from t0 in IgnoreText
			from g in GroupName
			from t1 in IgnoreText
			from h in HashAssignments
			select new TomlBlock(g, h);




	}

	public class TomlBlock:ITomlData
	{
		public TomlBlock(TomlKeyGroupName tomlKeyGroupName, TomlAssignments tomlAssignments)
		{
			throw new NotImplementedException();
		}
	}

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

	public interface ITomlData
	{
	}

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

	[DebuggerDisplay("{Debug()}")]
	public class TomlKeyValue
	{
		public string Key { get; private set; }
		public ITomlToken Value { get; private set; }

		public TomlKeyValue(string key, ITomlToken val)
		{
			Key = key;
			Value = val;
		}

		private string Debug()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Key).Append(" = ").Append(Value);
			return sb.ToString();
		}
	}

	public class DynamicToml : DynamicObject
	{

	}


}
