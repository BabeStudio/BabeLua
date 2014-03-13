using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Babe.Lua.TextMarker
{
    [Export(typeof(EditorFormatDefinition))]
    [Name("MarkerFormatDefinition/FindWordFormatDefination")]
    [UserVisible(true)]
    internal class FindWordFormatDefination : MarkerFormatDefinition
    {
        public FindWordFormatDefination()
        {
            //this.BackgroundColor = Color.FromArgb(50, 246, 185, 77);
            //this.BackgroundColor = Colors.White;
            
            this.Border = new Pen(Brushes.Blue, 1.0);
            this.DisplayName = "FindWord";
        }
    }
}
