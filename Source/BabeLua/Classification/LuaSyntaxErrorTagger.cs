using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using System.Threading;

namespace LuaLanguage.Classification
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(LuaErrorTag))]
    [ContentType("Lua")]
    internal sealed class LuaSyntaxErrorTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //create a single tagger for each buffer.
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(
                "LuaSyntaxErrorTagger", () => new LuaSyntaxErrorTagger(buffer) as ITagger<T>);
        }
    }

    internal sealed class LuaSyntaxErrorTagger : ITagger<LuaErrorTag>
    {
        ITextBuffer buffer;
        List<Irony.Parsing.Token> errorTokens = new List<Irony.Parsing.Token>();
        Timer delayTimer;
        String msgParse;

        static Irony.Parsing.Grammar grammar = LuaLanguage.LuaGrammar.Instance;
       
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public LuaSyntaxErrorTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;

			Babe.Lua.Editor.TextViewCreationListener.FileContentChanged += TextViewCreationListener_FileContentChanged;

			Irony.Parsing.Parser parser = new Irony.Parsing.Parser(LuaLanguage.LuaGrammar.Instance);
			var tree = parser.Parse(buffer.CurrentSnapshot.GetText());
			ReParse(tree);
        }

		private void TextViewCreationListener_FileContentChanged(object sender, Irony.Parsing.ParseTree e)
		{
			ReParse(e);
		}

        public IEnumerable<ITagSpan<LuaErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || this.errorTokens.Count == 0)
                yield break;

            List<Irony.Parsing.Token> currentErrors = this.errorTokens;
            ITextSnapshot currentSnapshot = spans[0].Snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;

            int count = 0;
            foreach (var error in currentErrors)
            {
                count++;
                if (error.Location.Line <= endLineNumber && error.Location.Line >= startLineNumber)
                {
                    var line = currentSnapshot.GetLineFromLineNumber(error.Location.Line);
                    var startPosition = error.Location.Position;

                    int length = error.Length;
                    if (length == 0)
                    {
                        length = 1;
                        //startPosition -= 1;
                    }
                    else
                    {
                        length = Math.Min(length, 100);
                        var len = currentSnapshot.Length - error.Location.Position - 1;
                        if (len > 0)
                            length = Math.Min(len, length);
                    }

                    var msg = error.ValueString;
                    if (count == currentErrors.Count() && msgParse != "")
                        msg = msgParse;

                    if (currentSnapshot.Length >= startPosition + length)
                    {
                        yield return new TagSpan<LuaErrorTag>(
                            new SnapshotSpan(currentSnapshot, startPosition, length),
                            new LuaErrorTag(msg));
                    }
                }
            }
        }

        private void ReParse(Irony.Parsing.ParseTree tree)
        {
            int previousCount = errorTokens.Count;
            errorTokens.Clear();

            ITextSnapshot newSnapshot = this.buffer.CurrentSnapshot;
            
            var newErrors = new List<Irony.Parsing.Token>();
            
			foreach (var token in tree.Tokens)
            {
                if (token.IsError())
                {
                    errorTokens.Add(token);
                }
            }

            if (tree.HasErrors())
            {
                var tok = tree.Tokens.Last();
                errorTokens.Add(tok);
                //if (tok.Length != 0)
                //    errorTokens.Add(tok);
                //else //it is EOF error so before the end(use -2)
                //    errorTokens.Add(tree.Tokens[tree.Tokens.Count - 2]);
                msgParse = tree.ParserMessages[0].ToString();
            }
            else
                msgParse = "";

            if (previousCount != 0 || errorTokens.Count != 0)
            {
                if (this.TagsChanged != null)
                    this.TagsChanged(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(newSnapshot, 0, newSnapshot.Length)));
            }
        }
    }

    internal class LuaErrorTag : ErrorTag
    {
        public LuaErrorTag() : base(PredefinedErrorTypeNames.SyntaxError) { }

        public LuaErrorTag(string message) : base(PredefinedErrorTypeNames.SyntaxError, message) { }
    }

    internal static class SyntaxErrorInfoExtensions
    {
        public static SnapshotSpan AsSnapshotSpan(this Irony.Parsing.Token token, ITextSnapshot snapshot)
        {
            return new SnapshotSpan(snapshot, token.Location.Position, Math.Max(token.Length, 1));
        }
    }
}
