using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Babe.Lua.Editor
{
    class MarkPosGlyphFactory : IGlyphFactory
    {
        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            const double m_glyphSize = 16.0;

            System.Windows.Shapes.Polygon polygon = new Polygon();
            polygon.Fill = Brushes.Blue;
            polygon.StrokeThickness = 0;
            polygon.Points.Add(new Point(0, 5));
            polygon.Points.Add(new Point(12, 5));
            polygon.Points.Add(new Point(16, 8));
            polygon.Points.Add(new Point(12, 11));
            polygon.Points.Add(new Point(0, 11));

            polygon.Width = m_glyphSize;
            polygon.Height = m_glyphSize;

            return polygon;
        }
    }
}
