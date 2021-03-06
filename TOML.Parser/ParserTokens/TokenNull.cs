﻿using System.Diagnostics;

namespace TOML.ParserTokens
{
	[DebuggerDisplay("null")]
	public class TokenNull:ITomlToken
	{
		public object Value { get { return null; } }

		public override string ToString()
		{
			return GetOutput();
		}

		public string GetOutput()
		{
			return "null";
		}
	}
}