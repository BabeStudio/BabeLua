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

    internal sealed class LuaSyntaxErrorTagger : ITagger<LuaErrorTag>, IDisposable
    {
        ITextBuffer buffer;
        //ITextSnapshot snapshot;
        List<Irony.Parsing.Token> errorTokens = new List<Irony.Parsing.Token>();
        Timer delayTimer;
        String msgParse;

        static Irony.Parsing.Grammar grammar = LuaLanguage.LuaGrammar.Instance;
       
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public LuaSyntaxErrorTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            //this.snapshot = buffer.CurrentSnapshot;
            this.ReParse();
            this.buffer.Changed += BufferChanged;
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

        private void ReParse()
        {
            int previousCount = errorTokens.Count;
            errorTokens.Clear();

            ITextSnapshot newSnapshot = this.buffer.CurrentSnapshot;
            string text = newSnapshot.GetText();

            Irony.Parsing.Parser parser = new Irony.Parsing.Parser(grammar);
            var newErrors = new List<Irony.Parsing.Token>();
            var parseTree = parser.Parse(text);
            foreach (var token in parseTree.Tokens)
            {
                if (token.IsError())
                {
                    errorTokens.Add(token);
                }
            }

            if (parseTree.HasErrors())
            {
                var tok = parseTree.Tokens.Last();
                errorTokens.Add(tok);
                //if (tok.Length != 0)
                //    errorTokens.Add(tok);
                //else //it is EOF error so before the end(use -2)
                //    errorTokens.Add(parseTree.Tokens[parseTree.Tokens.Count - 2]);
                msgParse = parseTree.ParserMessages[0].ToString();
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

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != buffer.CurrentSnapshot)
                return;

            if (delayTimer != null)
                delayTimer.Dispose();
            //在文本停止变化1秒后重新扫描
            delayTimer = new Timer(o => this.ReParse(), null, 1000, Timeout.Infinite);
        }

        #region IDisposable Members

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if(delayTimer != null)
                    delayTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
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
