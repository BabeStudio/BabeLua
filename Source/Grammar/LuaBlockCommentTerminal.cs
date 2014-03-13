using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using System.Text.RegularExpressions;

namespace LuaLanguage
{
    public class LuaCommentTerminal : Terminal
    {
        public LuaCommentTerminal(string name)
            : base(name, TokenCategory.Comment)
        {
            Priority = TerminalPriority.High;
            this.SetFlag(TermFlags.IsMultiline);
        }

        public string StartSymbol = "--";
       
        #region overrides
        public override void Init(GrammarData grammarData) 
        {
            base.Init(grammarData);
            
            if (this.EditorInfo == null) 
            {
               this.EditorInfo = new TokenEditorInfo(TokenType.Comment, TokenColor.Comment, TokenTriggers.None);
            }
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) 
        {
            Token result;
            if (context.VsLineScanState.Value != 0)
            {
                byte commentLevel = context.VsLineScanState.TokenSubType;
                result = CompleteMatch(context, source, commentLevel);
            } 
            else 
            {
                byte commentLevel = 0;
                if (!BeginMatch(context, source, ref commentLevel)) 
                    return null;

                result = CompleteMatch(context, source, commentLevel);
            }
            
            if (result != null) 
                return result;

            if (context.Mode == ParseMode.VsLineScan)
                return CreateIncompleteToken(context, source);

            return context.CreateErrorToken("unclosed comment");
        }

        private Token CreateIncompleteToken(ParsingContext context, ISourceStream source)
        {
            source.PreviewPosition = source.Text.Length;
            Token result = source.CreateToken(this.OutputTerminal);
            result.Flags |= TokenFlags.IsIncomplete;
            context.VsLineScanState.TerminalIndex = this.MultilineIndex;
            return result; 
        }

        private bool BeginMatch(ParsingContext context, ISourceStream source, ref byte commentLevel)
        {
            if (!source.MatchSymbol(StartSymbol)) 
                return false;

            string text = source.Text.Substring(source.PreviewPosition + StartSymbol.Length);
            var match = Regex.Match(text, @"^\[(=*)\[");
            if(match.Value != string.Empty)
            {
                commentLevel = (byte)(match.Groups[1].Value.Length + 1);
            }

            source.PreviewPosition += StartSymbol.Length + commentLevel;
           
            return true;
        }

        private Token CompleteMatch(ParsingContext context, ISourceStream source, byte commentLevel)
        {
            if (commentLevel == 0)
            {
                var line_breaks = new char[] { '\n', '\r', '\v' };
                var firstCharPos = source.Text.IndexOfAny(line_breaks, source.PreviewPosition);
                if (firstCharPos > 0)
                {
                    source.PreviewPosition = firstCharPos;
                }
                else
                {
                    source.PreviewPosition = source.Text.Length;
                }

                return source.CreateToken(this.OutputTerminal);
            }

            while (!source.EOF())
            {      
               string text = source.Text.Substring(source.PreviewPosition);
                var matches = Regex.Matches(text, @"\](=*)\]");
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value.Length == (int)commentLevel - 1)
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

                source.PreviewPosition++;
            }
            
			context.VsLineScanState.TokenSubType = commentLevel;
            return null;
        }

        public override IList<string> GetFirsts() 
        {
            return new string[] { StartSymbol };
        }
        #endregion
    }
}
