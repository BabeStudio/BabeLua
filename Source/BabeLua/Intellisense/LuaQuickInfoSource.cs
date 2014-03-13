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

using LuaLanguage.DataModel;

namespace LuaLanguage
{
    
    //[Export(typeof(IQuickInfoSourceProvider))]
    //[ContentType("Lua")]
    //[Name("luaQuickInfo")]
    //class LuaQuickInfoSourceProvider : IQuickInfoSourceProvider
    //{

    //    [Import]
    //    IBufferTagAggregatorFactoryService aggService = null;

    //    public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
    //    {
    //        return new LuaQuickInfoSource(textBuffer, aggService.CreateTagAggregator<ClassificationTag>(textBuffer));
    //    }
    //}

    //class LuaQuickInfoSource : IQuickInfoSource
    //{
    //    private ITagAggregator<ClassificationTag> _aggregator;
    //    private ITextBuffer _buffer;
    //    private bool _disposed = false;

    //    public LuaQuickInfoSource(ITextBuffer buffer, ITagAggregator<ClassificationTag> aggregator)
    //    {
    //        _aggregator = aggregator;
    //        _buffer = buffer;
    //    }
   

    //    public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
    //    {
    //        applicableToSpan = null;

    //        if (_disposed)
    //            throw new ObjectDisposedException("TestQuickInfoSource");

    //        var triggerPoint = (SnapshotPoint) session.GetTriggerPoint(_buffer.CurrentSnapshot);

    //        if (triggerPoint == null)
    //            return;

    //        foreach (IMappingTagSpan<ClassificationTag> curTag in _aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint)))
    //        {
    //            if (curTag.Tag.ClassificationType.Classification == "table")
    //            {
    //                //var tagSpan = curTag.Span.GetSpans(_buffer).First();
    //                //applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
    //                //quickInfoContent.Add("Lua table no need comment");
    //            }
    //            else if (curTag.Tag.ClassificationType.Classification == "function")
    //            {
    //                //find table name for it
    //                var tagSpan = curTag.Span.GetSpans(_buffer).First();
    //                applicableToSpan = _buffer.CurrentSnapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);
    //                var start = tagSpan.Start;

    //                string function = tagSpan.GetText();
    //                string table = "_G";
    //                if (start.Position > 1)
    //                {
    //                    start -= 1;
    //                    char ch = start.GetChar();
    //                    if (ch == '.' || ch == ':')
    //                    {
    //                        var pos = start;
    //                        while (pos.Position > 0 && (pos - 1).GetChar().IsWord())
    //                        {
    //                            pos -= 1;
    //                        }                                
    //                        ITextSnapshot snapshot = _buffer.CurrentSnapshot;
    //                        table = snapshot.GetText(pos.Position, start.Position - pos.Position);
    //                    }
    //                }

    //                //quickInfoContent.Add(s);
    //            }
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        _disposed = true;
    //    }
    //}
}

