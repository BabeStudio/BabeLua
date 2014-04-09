using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Babe.Lua.Package;

namespace Babe.Lua
{
    [Guid(GuidList.OutlineWindowString)]
    public class OutlineWndPane : ToolWindowPane
    {
        public static OutlineWndPane Current;
        ToolWindows.OutlineWindow wnd;
        
        public OutlineWndPane() :
            base(null)
        {
            this.Caption = Properties.Resources.OutlineWindowTitle;
            
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;
            this.ToolBarLocation = 2;

            this.wnd = new ToolWindows.OutlineWindow();
            base.Content = wnd;

            //this.Frame = DTEHelper.Current.DTE.MainWindow.LinkedWindowFrame;
            
            Current = this;
            
        }

        public void Refresh()
        {
            wnd.Dispatcher.Invoke(() => wnd.Refresh());
        }

        public void SetFocus()
        {
            wnd.TextBox_Search.SelectAll();
            wnd.TextBox_Search.Focus();
        }

        public void LostFocus()
        {
            if (wnd.ListBox_SearchResult.Visibility == Visibility.Visible)
            {
                wnd.ListBox_SearchResult.Visibility = Visibility.Collapsed;
                wnd.TextBox_Search.Text = "Search File";
            }
        }
    }
}
