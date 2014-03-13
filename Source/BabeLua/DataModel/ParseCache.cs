using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Babe.Lua.DataModel
{
    class ParseCache
    {
        Dictionary<ITextView, ParseTree> Trees;

        public ParseTree GetParseTree(ITextView textView)
        {
            if (Trees.ContainsKey(textView)) return Trees[textView];

            return Parse(textView);
        }

        public ParseTree Parse(ITextView textView)
        {
            var parse = new Parser(LuaLanguage.LuaGrammar.Instance);
            var tree = parse.Parse(textView.TextSnapshot.GetText());

            if (Trees.ContainsKey(textView))
            {
                Trees[textView] = tree;
            }
            else
            {
                Trees.Add(textView, tree);
            }

            return tree;
        }
    }
}
