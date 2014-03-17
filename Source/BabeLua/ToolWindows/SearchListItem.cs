using Babe.Lua.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Babe.Lua
{
    class SearchListItem
    {
        public string Left { get; set; }
        public string Right { get; set; }
        public string Highlight { get; set; }
        public LuaMember token { get; set; }

        public Brush Background
        {
            get;
            set;
        }

        public SearchListItem() { }

        public SearchListItem(LuaMember lm, string id)
        {
            this.token = lm;

            if (lm.Preview == null)
            {
                Left = lm.ToString();
				Highlight = string.Empty;
				Right = string.Empty;
            }
            else
            {
                Left = string.Format("{4} : {0} - ({1},{2}) : {3}", lm.File.File, lm.Line + 1, lm.Column + 1, lm.Preview.Substring(0, lm.Column).TrimStart(), id);
                Highlight = lm.Name;
                Right = lm.Preview.Substring(lm.Column + lm.Name.Length);
            }
        }

        public SearchListItem(string text, string highlight)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(highlight)) return;
            int index = text.IndexOf(highlight, StringComparison.OrdinalIgnoreCase);
            if (index == -1) return;
            Left = text.Substring(0, index);
            Highlight = highlight;
            Right = text.Substring(index + Highlight.Length);
        }

		public override string ToString()
		{
			return string.Concat(Left, Highlight, Right);
		}
    }
}
