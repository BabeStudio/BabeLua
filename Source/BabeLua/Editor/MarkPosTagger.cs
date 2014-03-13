using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Babe.Lua.Editor
{
    class MarkPosTag : IGlyphTag
    {
    }

    [Export(typeof(IViewTaggerProvider))]
    [ContentType("Lua")]
    [TagType(typeof(MarkPosTag))]
    class MarkPosTaggerProvider : IViewTaggerProvider
    {
        void textView_Closed(object sender, EventArgs e)
        {
            Taggers.Remove(sender as ITextView);
        }

        void textView_GotAggregateFocus(object sender, EventArgs e)
        {
            CurrentTagger = Taggers[sender as ITextView];
        }

        public static MarkPosTagger CurrentTagger { get; private set; }
        Dictionary<ITextView, MarkPosTagger> Taggers = new Dictionary<ITextView, MarkPosTagger>();

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer)
                return null;

            if (Taggers.ContainsKey(textView))
            {
                CurrentTagger = Taggers[textView];
            }
            else
            {
                CurrentTagger = new MarkPosTagger(textView);
                Taggers.Add(textView, CurrentTagger);

                textView.GotAggregateFocus += textView_GotAggregateFocus;
                textView.Closed += textView_Closed;
            }

            return CurrentTagger as ITagger<T>;
        }
    }

    internal class MarkPosTagger : ITagger<MarkPosTag>
    {
        private ITextView m_view;
        private int ShowLine = -1;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        internal MarkPosTagger(ITextView view)
        {
            m_view = view;
            m_view.Caret.PositionChanged += (s, e) => HideTag();
        }

        public void ShowTag(int line)
        {
            if (ShowLine == line) return;

            if (m_view.TextSnapshot.LineCount <= line)
            {
                return;
            }

            ShowLine = line;

            if (TagsChanged != null)
            {
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(m_view.TextSnapshot.GetLineFromLineNumber(ShowLine).Start, 0)));
            }
        }

        public void HideTag()
        {
            if (ShowLine == -1) return;
            int line = ShowLine;
            ShowLine = -1;
            if (TagsChanged != null)
            {
				TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(m_view.TextSnapshot, m_view.TextSnapshot.GetLineFromLineNumber(line).Start, 0)));
            }
        }

        IEnumerable<ITagSpan<MarkPosTag>> ITagger<MarkPosTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (ShowLine != -1)
            {
                if (spans.Count == 0)
                    yield break;
                var SnapShot = spans[0].Snapshot;

                foreach (var span in spans)
                {
                    if(SnapShot.GetLineNumberFromPosition(span.Start.Position) == ShowLine)
                    {
                        yield return new TagSpan<MarkPosTag>(span, new MarkPosTag());
                    }
                }
            }
        }
    }
}
