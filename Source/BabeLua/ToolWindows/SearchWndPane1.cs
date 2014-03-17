using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Babe.Lua.DataModel;

namespace Babe.Lua
{
    [Guid(GuidList.SearchWindowString1)]
    public class SearchWndPane1 : ToolWindowPane,ISearchWnd
    {
        SearchToolControl wnd;

        public static SearchWndPane1 Current;
        
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public SearchWndPane1() :
            base(null)
        {
            this.Caption = Properties.Resources.SearchlWindowTitle1;
            
            this.BitmapResourceID = 301;

            this.BitmapIndex = 1;

            wnd = new SearchToolControl();
            base.Content = wnd;
            Current = this;
        }

        public void Search(string txt, bool AllFile)
        {
            wnd.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(txt))
                {
                    this.Caption = Properties.Resources.SearchlWindowTitle1;
					wnd.ListView.Items.Clear();
                }
                else
                {
					var list = FileManager.Instance.FindAllRef(txt, AllFile);
					this.Caption = string.Format("{0} - find {1} matches", Properties.Resources.SearchlWindowTitle1, list.Count);
					wnd.Refresh(list);
                }
            });
        }
    }

    interface ISearchWnd
    {
        void Search(string txt, bool AllFile);
    }
}
