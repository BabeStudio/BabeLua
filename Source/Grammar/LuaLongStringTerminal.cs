using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using System.Text.RegularExpressions;

namespace Grammar
{
    class LuaLongStringTerminal : Terminal
    {
        public LuaLongStringTerminal(string name)
            : base(name, TokenCategory.Content)
        {
            this.SetFlag(TermFlags.IsMultiline);
        }

        public string StartSymbol = "[";

        #region overrides
        public override void Init(GrammarData grammarData) 
        {
            base.Init(grammarData);
            
            if (this.EditorInfo == null) 
            {
               this.EditorInfo = new TokenEditorInfo(TokenType.String, TokenColor.String, TokenTriggers.None);
            }
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) 
        {
            Token result;
            if (context.VsLineScanState.Value != 0)
            {
                byte level = context.VsLineScanState.TokenSubType;
                result = CompleteMatch(context, source, level);
            } 
            else 
            {
                byte level = 0;
                if (!BeginMatch(context, source, ref level)) 
                    return null;

                result = CompleteMatch(context, source, level);
            }
            
            if (result != null) 
                return result;

            if (context.Mode == ParseMode.VsLineScan)
                return CreateIncompleteToken(context, source);

            this.Category = TokenCategory.Error;
            
			return context.CreateErrorToken("unclosed comment block!");
        }

        private Token CreateIncompleteToken(ParsingContext context, ISourceStream source)
        {
            source.PreviewPosition = source.Text.Length;
            Token result = source.CreateToken(this.OutputTerminal);
            result.Flags |= TokenFlags.IsIncomplete;
            context.VsLineScanState.TerminalIndex = this.MultilineIndex;
            return result; 
        }

        private bool BeginMatch(ParsingContext context, ISourceStream source, ref byte level)
        {
            if (!source.MatchSymbol(StartSymbol)) 
                return false;

            string text = source.Text.Substring(source.PreviewPosition + StartSymbol.Length);
            var match = Regex.Match(text, @"^(=*)\[");
            if(match.Value != string.Empty)
            {
                level = (byte)match.Groups[1].Value.Length;
                return true;
            }
           
            return false; 
        }

        private Token CompleteMatch(ParsingContext context, ISourceStream source, byte level)
        {
            string text = source.Text.Substring(source.PreviewPosition);
            var matches = Regex.Matches(text, @"\](=*)\]");
            foreach(Match match in matches) 
            {
                if (match.Groups[1].Value.Length == (int)level)
                {
                    source.PreviewPosition += match.Index + match.Length;


                    if (context.VsLineScanState.Value != 0)
                    {
                        SourceLocation tokenStart = new SourceLocation();
                        tokenStart.Position = 0;

                        string lexeme = source.Text.Substring(0, source.PreviewPosition);

                        context.VsLineScanState.Value = 0;
                        return new Token(this, tokenStart, lexeme, null);
                    }
                    else
                    {
                        return source.CreateToken(this.OutputTerminal); 
                    }
                }
            }

            context.VsLineScanState.TerminalIndex = this.MultilineIndex;
            context.VsLineScanState.TokenSubType = level;
            return null;
        }

        public override IList<string> GetFirsts() 
        {
            return new string[] { StartSymbol };
        }

        #endregion
    }
}
