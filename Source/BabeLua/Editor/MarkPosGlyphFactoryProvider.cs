using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Editor
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("TodoGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("Lua")]
    [TagType(typeof(MarkPosTag))]
    internal sealed class MarkPosGlyphFactoryProvider : IGlyphFactoryProvider
    {

        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new MarkPosGlyphFactory();
        }
    }
}
