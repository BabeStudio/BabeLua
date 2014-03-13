using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Editor
{
    class EditorMargin : IWpfTextViewMargin
    {
        public System.Windows.FrameworkElement VisualElement
        {
            get 
            {
                return control;
            }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return this;
        }

        public double MarginSize
        {
            get { return 22; }
        }

        public void Dispose()
        {
            
        }

        OutlineMarginControl control;

        public EditorMargin(IWpfTextViewHost TextViewHost)
        {
            control = new OutlineMarginControl(TextViewHost);
        }

        public void OpenLeftOutline()
        {
            control.ComboBox_Table.IsDropDownOpen = true;
            control.ComboBox_Table.Focus();
        }

        public void OpenRightOutline()
        {
            control.ComboBox_Member.IsDropDownOpen = true;
            control.ComboBox_Member.Focus();
        }

        public void Refresh()
        {
            control.Dispatcher.Invoke(() => control.Refresh());
        }
    }

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("EditorMargin")]
    [Order(Before = PredefinedMarginNames.HorizontalScrollBar)] //Ensure that the margin occurs below the horizontal scrollbar
    [MarginContainer(PredefinedMarginNames.Top)] //Set the container to the bottom of the editor window
    [ContentType("Lua")] //Show this margin for all text-based types
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    class EditorMarginProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            DTEHelper.Current.SelectionPage = wpfTextViewHost.TextView.Selection;

            CurrentMargin = new EditorMargin(wpfTextViewHost);
            OutlineMargins.Add(wpfTextViewHost.TextView, CurrentMargin);

            wpfTextViewHost.TextView.Closed += TextView_Closed;
            wpfTextViewHost.TextView.GotAggregateFocus += TextView_GotAggregateFocus;

            return CurrentMargin;
        }

        void TextView_GotAggregateFocus(object sender, EventArgs e)
        {
            CurrentMargin = OutlineMargins[sender as ITextView];
        }

        void TextView_Closed(object sender, EventArgs e)
        {
            OutlineMargins.Remove(sender as ITextView);
        }

        public static EditorMargin CurrentMargin { get; private set; }
        Dictionary<ITextView, EditorMargin> OutlineMargins = new Dictionary<ITextView, EditorMargin>();
    }
}
