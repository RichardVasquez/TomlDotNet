using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TOML
{
	[DebuggerDisplay("{Value}")]
	public class TokenDateTime:ITomlToken
	{
		private static readonly Dictionary<int, List<int>> Days = new Dictionary<int, List<int>>
		{
			{28, new List<int> {2}},
			{30, new List<int> {1, 3, 5, 7, 8, 10, 12}},
			{31, new List<int> {4, 6, 9, 11}}
		};

		public DateTime Value { get; private set; }

		public TokenDateTime(IList<string> components)
		{
			if (components == null || components.Count != 6)
			{
				throw new ArgumentException("TokenDateTime components length.");
			}

			List<int> parts = new List<int>();
			try
			{
				int idx = 0;
				parts.AddRange(components.Select(component => ConvertPart(component, idx++)));
				Value = DateConvert(parts);
			}
			catch(Exception e)
			{
				throw new ArgumentException(
					string.Format(
					              "TokenDateTime components value: {0}-{1}-{2}T{3}:{4}:{5}Z",
					              components[ 0 ], components[ 1 ], components[ 2 ], components[ 3 ],
					              components[ 4 ], components[ 5 ]
						), e);
			}
		}

		/// <summary>
		/// Creates a Zulu based DateTime if incoming is valid.
		/// </summary>
		private static DateTime DateConvert(IList<int> parts)
		{
			if (
				CheckYear(parts[ 0 ], parts[ 1 ], parts[ 2 ]) &&
				CheckTime(parts[ 3 ], parts[ 4 ], parts[ 5 ])
				)
			{
				return new DateTime(
					parts[ 0 ], parts[ 1 ], parts[ 2 ], parts[ 3 ], parts[ 4 ], parts[ 5 ], DateTimeKind.Utc);
			}
			throw new ArgumentException("Invalid date time format");
		}

		/// <summary>
		/// Checks the time, allows for 24:00:00, but no other 24:XX:XX values.
		/// </summary>
		private static bool CheckTime(int hour, int minute, int second)
		{
			if (hour == 24 && minute == 0 && second == 0)
			{
				return true;
			}

			return hour != 24;
		}

		/// <summary>
		/// Test valid date number for month with leap year check.
		/// </summary>
		private static bool CheckYear(int year, int month, int day)
		{
			if (DateTime.IsLeapYear(year))
			{
				if (month == 2 && day == 29)
				{
					return true;
				}
			}

			int max = Days.Keys.FirstOrDefault(key => Days[ key ].Contains(month));
			if (!Days.ContainsKey(max))
			{
				return false;
			}

			return day <= max;
		}

		/// <summary>
		/// Checks if initial text can be converted to an integer and
		/// if it's in the proper range for its position.
		/// </summary>
		private static int ConvertPart(string component, int i)
		{
			switch (i)
			{
				case 0:
					return MakeValue(component, "year", 4, 0, 9999);
				case 1:
					return MakeValue(component, "month", 2, 1, 12);
				case 2:
					return MakeValue(component, "day", 2, 1, 31);
				case 3:
					return MakeValue(component, "hour", 2, 0, 24);
				case 4:
					return MakeValue(component, "minute", 2, 0, 59);
				case 5:
					return MakeValue(component, "second", 2, 0, 59);
				default:
					throw new ArgumentException("component index");
			}
		}

		/// <summary>
		/// Tests an incoming string and sees if it converts to an integer and
		/// if it falls within the valid range allowed.
		/// </summary>
		private static int MakeValue(string text, string name, int length, int min, int max)
		{
			if (text.Length != length)
			{
				throw new ArgumentException(string.Format("Bad {0} length", name));
			}

			int i;
			if (!int.TryParse(text, out i))
			{
				throw new ArgumentException(string.Format( "Bad {0} format", name));
			}

			if (i < min || i > max)
			{
				throw new ArgumentException(string.Format("Bad {0} value", name));
			}

			return i;
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		public string GetOutput()
		{
			return Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
		}
	}
}