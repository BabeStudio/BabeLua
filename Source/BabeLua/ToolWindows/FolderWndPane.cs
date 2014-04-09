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
    [Guid(GuidList.FolderWindowString)]
    public class FolderWndPane : ToolWindowPane
    {
        ToolWindows.FolderWindow wnd;
        public static FolderWndPane Current;
        
        public FolderWndPane() :
            base(null)
        {
            this.Caption = Properties.Resources.FolderWindowTitle;
            
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            wnd = new ToolWindows.FolderWindow();
            base.Content = wnd;

            //this.Frame = DTEHelper.Current.DTE.MainWindow.LinkedWindowFrame;
            
            Current = this;
        }

        public void Refresh()
        {
            wnd.Refresh();
        }

        public void SetFocus()
        {
            wnd.TextBox.Focus();
            wnd.TextBox.SelectAll();
        }

        public void LostFocus()
        {
            if (wnd.SearchView.Visibility == Visibility.Visible)
            {
                wnd.SearchView.Visibility = Visibility.Collapsed;
                wnd.TextBox.Text = "Search File";
            }
        }
    }
}
