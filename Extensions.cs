using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DH.Extensions
{
	public static class Extensions
	{
		public static bool Contains(this string source, string contained, StringComparison comparisonType)
		{
			return source.IndexOf(contained, comparisonType) >= 0;
		}
	}
}
