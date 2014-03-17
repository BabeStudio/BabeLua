using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Threading;

using Grammar;

namespace Babe.Lua.Intellisense
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("Lua")] 
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            Func<ITagger<T>> sc = delegate() { return new OutliningTagger(buffer) as ITagger<T>; };
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        } 
    }
        
    internal sealed class OutliningTagger : ITagger<IOutliningRegionTag>
    {
        string ellipsis = "...";    //the characters that are displayed when the region is collapsed
        ITextBuffer buffer;
        ITextSnapshot snapshot;
        List<Region> regions;

        public OutliningTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
            
			Babe.Lua.Editor.TextViewCreationListener.FileContentChanged += TextViewCreationListener_FileContentChanged;

			Irony.Parsing.Parser parser = new Irony.Parsing.Parser(LuaGrammar.Instance);
			var tree = parser.Parse(snapshot.GetText());
			ReParse(tree);
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;
            List<Region> currentRegions = this.regions;
            ITextSnapshot currentSnapshot = spans[0].Snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;
            foreach (var region in currentRegions)
            {
                if (region.StartLine <= endLineNumber &&
                    region.EndLine >= startLineNumber)
                {
                    var n = currentSnapshot.LineCount;
                    if (region.StartLine >= 0 && region.StartLine < n && region.EndLine >= 0 && region.EndLine < n)
                    {
                        var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                        var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                        var startPosition = startLine.Start.Position + region.StartOffset;
                        var length = (endLine.Start.Position + region.EndOffset) - startPosition;

                        //the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
                        if (length > 0 && currentSnapshot.Length >= startLine.Start.Position + region.StartOffset + length)
                        {
                            var preview = region.Preview == null ? ellipsis : region.Preview;
                            yield return new TagSpan<IOutliningRegionTag>(
                            new SnapshotSpan(currentSnapshot,
                                startLine.Start.Position + region.StartOffset, length),
                            new OutliningRegionTag(region.IsCollapsed, false, preview, String.Empty));
                        }
                    }
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		void TextViewCreationListener_FileContentChanged(object sender, Irony.Parsing.ParseTree e)
		{
			ReParse(e);
		}

        void ReParse(Irony.Parsing.ParseTree tree)
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();

            if (tree.Root != null)
            {
                FindHiddenRegions(newSnapshot, tree.Root, ref newRegions);

                FindUserRegions(newSnapshot, tree, ref newRegions);

            }
          
            //determine the changed span, and send a changed event with the new spans
            List<Span> oldSpans =
                new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
                    .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                    .Span));
            List<Span> newSpans =
                    new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

            NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

            //the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed =
            NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

            this.snapshot = newSnapshot;
            this.regions = newRegions;

            if (changeStart <= changeEnd)
            {
                ITextSnapshot snap = this.snapshot;
                if (this.TagsChanged != null)
                    this.TagsChanged(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        void FindHiddenRegions(ITextSnapshot snapShot, Irony.Parsing.ParseTreeNode root, ref List<Region> regions)
        {
            foreach (var child in root.ChildNodes)
            {
                Irony.Parsing.Token startToken = null;
                Irony.Parsing.Token endToken = null;
                int startOffset = 0;

                if (child.Term.Name == LuaTerminalNames.TableConstructor)
                {
                    startToken = child.ChildNodes.First().Token;    // '{' symbol
                    endToken = child.ChildNodes.Last().Token;       // '}' symbol
                }
                else if (child.Term.Name == LuaTerminalNames.FunctionBody)
                {
                    startToken = child.ChildNodes[2].Token; // ')' symbol
                    endToken = child.ChildNodes[4].Token;   // 'end' keyword
                    
                    //Offset the outline by 1 so we don't hide the ')' symbol.
                    startOffset = 1;
                }
                ////折叠连续的多个注释块
                //else if (child.Token != null && child.Comments != null)
                //{
                //    if (child.Comments.Count > 1)
                //    {
                //        startToken = child.Comments[0];
                //        endToken = child.Comments.Last();
                //        startOffset = startToken.Text.Length;
                //    }
                //}
                
                if (startToken != null && endToken != null)
                {
                    if (startToken.Location.Line != endToken.Location.Line)
                    {
                        //So the column field of Location isn't always accurate...
                        //Position and Line are accurate though..
                        var startLine = snapShot.GetLineFromLineNumber(startToken.Location.Line);
                        var startLineOffset = startToken.Location.Position - startLine.Start.Position;

                        var endLine = snapShot.GetLineFromLineNumber(endToken.Location.Line);
                        var endLineOffset = (endToken.Location.Position + endToken.Length) - endLine.Start.Position;
                        
                        var region = new Region();
                        region.StartLine = startToken.Location.Line;
                        region.StartOffset = startLineOffset + startOffset;

                        region.EndLine = endToken.Location.Line;
                        region.EndOffset = endLineOffset;

                        regions.Add(region);
                    }      
                }

                FindHiddenRegions(snapShot, child, ref regions);
            }
        }

        /// <summary>
        /// 匹配自定义折叠标签，类似  --region [title]  和--endregion之间的内容；
        /// 匹配多行注释，类似--[[ content --]]
        /// </summary>
        /// <param name="root"></param>
        /// <param name="regions"></param>
        void FindUserRegions(ITextSnapshot snapShot, Irony.Parsing.ParseTree tree, ref List<Region> regions)
        {
            Irony.Parsing.Token startRegion = null;

            foreach (var token in tree.Tokens)
            {
                Region region = null;

                if (token.Category == Irony.Parsing.TokenCategory.Comment)
                {
                    if (token.Text.Contains('\n'))//多行注释，折叠
                    {
                        region = new Region();
                        region.StartLine = token.Location.Line;
                        region.StartOffset = 0;

                        region.EndLine = token.Location.Line + token.Text.Count(c=>{return c == '\n';});
                        region.EndOffset = snapshot.GetLineFromLineNumber(region.EndLine).Length;

                        region.Preview = snapshot.GetLineFromLineNumber(region.StartLine).GetText().Replace("--[[", "");

                        region.IsCollapsed = true;
                    }
                    else
                    {
                        if (token.Text.StartsWith("--region ") && startRegion == null)
                        {
                            startRegion = token;
                        }

                        else if (token.Text.StartsWith("--endregion") && startRegion != null)
                        {
                            region = new Region();
                            var startLine = snapShot.GetLineFromLineNumber(startRegion.Location.Line);
                            var startLineOffset = startRegion.Location.Position - startLine.Start.Position;

                            var endLine = snapShot.GetLineFromLineNumber(token.Location.Line);
                            var endLineOffset = (token.Location.Position + token.Length) - endLine.Start.Position;

                            region.StartLine = startRegion.Location.Line;
                            region.StartOffset = startLineOffset;

                            region.EndLine = token.Location.Line;
                            region.EndOffset = endLineOffset;

                            region.Preview = startRegion.Text.Replace("--region ", "");
                            
                            startRegion = null;
                        }
                    }

                    if (region != null)
                    {
                        regions.Add(region);
                    }
                }
            }
        }

        static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine) ? startLine
                 : snapshot.GetLineFromLineNumber(region.EndLine);
            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
        }

        class Region
        {
            public int StartLine { get; set; }
            public int StartOffset { get; set; }
            public int EndLine { get; set; }
            public int EndOffset { get; set; }
            public string Preview { get; set; }
            public bool IsCollapsed { get; set; }
        }
    }
}
