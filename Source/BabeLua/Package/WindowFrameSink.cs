using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Package
{
    public class WindowFrameSink : IVsWindowFrameNotify
    {
        ISearchWnd wnd;

        internal WindowFrameSink(ISearchWnd wnd)
        {
            this.wnd = wnd;
        }

        int IVsWindowFrameNotify.OnDockableChange(int fDockable)
        {
            return VSConstants.S_OK;
        }

        int IVsWindowFrameNotify.OnMove()
        {
            return VSConstants.S_OK;
        }

        int IVsWindowFrameNotify.OnShow(int fShow)
        {
            switch (fShow)
            {
                case (int)__FRAMESHOW.FRAMESHOW_WinHidden:
                    DTEHelper.Current.SearchWndClosed(wnd);
                    break;
                default:
                    break;
            }
            return VSConstants.S_OK;
        }

        int IVsWindowFrameNotify.OnSize()
        {
            return VSConstants.S_OK;
        }
    }
}
