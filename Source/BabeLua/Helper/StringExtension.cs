using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace System
{
    static class StringExtension
    {
        public static bool Filter(this String source, String search)
        {
            if (String.IsNullOrWhiteSpace(source))
            {
                return false;
            }
            if (String.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            // 不区分大小写
            source = source.ToLowerInvariant();
            search = search.ToLowerInvariant();

            if (source.Contains(search)) return true;
            else return false;

			//int index = source.IndexOf(search[0]);
			//if (index < 0) return false;

			//for (short i = 1; i < search.Length; i++)
			//{
			//	char ch = search[i];
			//	index = source.IndexOf(ch, index + 1);
			//	if (index < 0)
			//	{
			//		return false;
			//	}
			//}
			//return true;
        }

        public static bool IsWordOrDot(this char c)
        {
            return IsWord(c) || c == '.';
        }

        public static bool IsWord(this char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || (c >= 0x4e00 && c <= 0x9fff);
        }
    }
}
