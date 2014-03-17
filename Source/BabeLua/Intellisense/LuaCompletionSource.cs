using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

using Babe.Lua.DataModel;
using Microsoft.VisualStudio.Text.Operations;

namespace Babe.Lua
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("Lua")]
    [Name("LuaCompletion")]
    class LuaCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new LuaCompletionSource(this, textBuffer);
        }
    }

    class LuaCompletionSource : ICompletionSource
    {
        LuaCompletionSourceProvider _provider;
        private ITextBuffer _buffer;
        private bool _disposed = false;
        
        //each table functions

        public LuaCompletionSource(LuaCompletionSourceProvider provider, ITextBuffer buffer)
        {
            _buffer = buffer;
            _provider = provider;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                throw new ObjectDisposedException("LuaCompletionSource");
         
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);

            if (triggerPoint == null)
                return;

            var line = triggerPoint.GetContainingLine();
            SnapshotPoint start = triggerPoint;

            var word = start;
            word -= 1;
            var ch = word.GetChar();

            List<Completion> completions = new List<Completion>();

            while (word > line.Start && (word - 1).GetChar().IsWordOrDot())
            {
                word -= 1;
            }

            if (ch == '.' || ch == ':')
            {
                String w = snapshot.GetText(word.Position, start - 1 - word);
                if (!FillTable(w, ch, completions)) return;
            }
            else
            {
                char front = word > line.Start ? (word - 1).GetChar() : char.MinValue;
                if (front == '.' || front == ':')
                {
                    int loc = (word - 1).Position;
                    while (loc > 0 && snapshot[loc - 1].IsWordOrDot()) loc--;
                    int len = word - 1 - loc;
                    if (len <= 0) return;
                    string w = snapshot.GetText(loc, len);
                    if (!FillTable(w, front, completions)) return;
                }
                else
                {
                    String w = snapshot.GetText(word.Position, start - word);
                    if (!FillWord(w, completions)) return;
                }
            }

            if (ch != '.' && ch != ':')
            {
                start = word;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
            var cs = new LuaCompletionSet("All", "All", applicableTo, completions, null);
            completionSets.Add(cs);
        }

        public bool FillWord(String word, List<Completion> completions)
        {
            int count = completions.Count;
            //提示全局索引
            var list = IntellisenseHelper.GetGlobal();
            //提示当前文件单词
            var tokens = IntellisenseHelper.GetFileTokens();

            list = list.Concat(tokens).Distinct();

            foreach (LuaMember s in list)
            {
                completions.Add(new Completion(s.Name, s.Name, s.GetType().Name + ":" + s.ToString(), null, "icon"));
            }

            return completions.Count > count;
        }

        public bool FillTable(String word, char dot, List<Completion> completions)
        {
            var count = completions.Count;

            var table = IntellisenseHelper.GetTable(word);

			if (table != null)
            {
				var result = table.GetFullMembers();
                if (dot == '.')
                {
					foreach (var list in result)
					{
						foreach (LuaMember l in list.Value)
						{
							completions.Add(new Completion(l.Name, l.Name, list.Key + dot + l.ToString(), null, "icon"));
						}
					}
                }
                else
                {
					foreach (var list in result)
					{
						foreach (LuaMember l in list.Value)
						{
							if (l is LuaFunction)
							{
								completions.Add(new Completion(l.Name, l.Name, list.Key + dot + l.ToString(), null, "icon"));
							}
						}
					}
                }
            }
			//else //找不到table。拿文件单词进行提示。
			//{
			//	var tokens = IntellisenseHelper.GetFileTokens();
			//	foreach (LuaMember lm in tokens)
			//	{
			//		completions.Add(new Completion(lm.Name, lm.Name, lm.Name, null, "icon"));
			//	}
			//}

            return completions.Count > count;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    class LuaCompletionSet : CompletionSet
    {
        public LuaCompletionSet(string moniker,
            string displayName,
            ITrackingSpan applicableTo,
            IEnumerable<Completion> completions,
            IEnumerable<Completion> completionBuilders)
            : base(moniker, displayName, applicableTo, completions, completionBuilders)
        {

        }

        public override void SelectBestMatch()
        {
            this.SelectBestMatch(CompletionMatchType.MatchDisplayText, true);
        }

        public override void Filter()
        {
            base.Filter(CompletionMatchType.MatchDisplayText, false);
        }

        public override void Recalculate()
        {
            base.Recalculate();
        }
    }
}

