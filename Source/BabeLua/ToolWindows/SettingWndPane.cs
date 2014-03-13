using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Babe.Lua
{
    [Guid(GuidList.SettingWindowString)]
    public class SettingWndPane : ToolWindowPane
    {
        ToolWindows.SettingWindow wnd;
        public static SettingWndPane Current;

        public SettingWndPane() :
            base(null)
        {
            this.Caption = Properties.Resources.SettingWindotTitle;
            
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            wnd = new ToolWindows.SettingWindow();
            base.Content = wnd;

            Current = this;
        }

        public void ShowToLuaFolder()
        {
            wnd.TextBox_LuaPath.SelectAll();
            wnd.TextBox_LuaPath.Focus();
        }

        public void ShowToExecutable()
        {
            wnd.TextBox_LuaExecutablePath.Focus();
        }
    }
}
