using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.Package
{
    class OleComponent : IOleComponent 
    {
        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
        {
            return 1;
        }

        public int FDoIdle(uint grfidlef)
        {
            return 0;
        }

        public int FPreTranslateMessage(MSG[] pMsg)
        {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser)
        {
            return 1;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
        {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
        {
            
        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID)
        {
            //if (fActive == 1)
            //{
            //    DTEHelper.Current.RefreshFolderWnd();
            //}
        }

        public void OnEnterState(uint uStateID, int fEnter)
        {
            
        }

        public void OnLoseActivation()
        {
            
        }

        public void Terminate()
        {
            
        }
    }
}
