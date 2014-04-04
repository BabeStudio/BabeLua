using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

using Babe.Lua.DataModel;
using System.Threading;
using Babe.Lua;

namespace Babe.Lua.Intellisense
{
    internal sealed class CompletionCommandFilter : IOleCommandTarget
    {
        ICompletionSession _currentSession;

        public CompletionCommandFilter(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;

            Broker = broker;
        }

        
        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            bool handled = false;
            int hresult = VSConstants.S_OK;

            // 1. Pre-process
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.RETURN:
                        handled = Complete(false);
                        break;
                    case VSConstants.VSStd2KCmdID.TAB:
                        handled = Complete(false);
                        break;
                    case VSConstants.VSStd2KCmdID.CANCEL:
                        handled = Cancel();
                        break;
                }
            }

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			if (ErrorHandler.Succeeded(hresult))
			{
				if (pguidCmdGroup == VSConstants.VSStd2K)
				{
					switch ((VSConstants.VSStd2KCmdID)nCmdID)
					{
						case VSConstants.VSStd2KCmdID.TYPECHAR:
							char ch = GetTypeChar(pvaIn);
							if (ch == '.' || ch == ':')
							{
								Cancel();
								StartSession();
							}
							else if (!ch.IsIdentifier())
								Cancel();
							else if (_currentSession == null)
								StartSession();
							else if (_currentSession != null)
								Filter(false);
							break;
						case VSConstants.VSStd2KCmdID.BACKSPACE:
						case VSConstants.VSStd2KCmdID.DELETE:
							if (ShouldCancel())
								Cancel();
							else
							Filter(true);
							break;
					}
				}
			}

            return hresult;
        }

        private void Filter(bool back)
        {
            if (_currentSession == null)
                return;

			_currentSession.Filter();
			//if (back) _currentSession.Recalculate();

			//if (back)
			//{
			//	_currentSession.SelectedCompletionSet.Filter();
			//	_currentSession.SelectedCompletionSet.SelectBestMatch();
			//}
			//else
			//{
			//	_currentSession.SelectedCompletionSet.SelectBestMatch();
			//	if (_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected)
			//	{
			//		_currentSession.SelectedCompletionSet.Filter();
			//		_currentSession.SelectedCompletionSet.SelectBestMatch();
			//	}
			//}
        }

        bool Cancel()
        {
            if (_currentSession == null)
                return false;
            try
            {
                _currentSession.Dismiss();

                return true;
            }
            catch { return false; }
        }

        bool Complete(bool force)
        {
            if (_currentSession == null)
                return false;

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return false;
            }
            else
            {
                _currentSession.Commit();
                return true;
            }
        }

        bool StartSession()
        {
            if (_currentSession != null)
                return false;

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                _currentSession = Broker.TriggerCompletion(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                _currentSession = Broker.GetSessions(TextView)[0];
            }

			if (_currentSession != null)
			{
				_currentSession.Dismissed += (sender, args) =>
				{
					_currentSession = null;
				};
				_currentSession.Committed += (sender, args) => _currentSession = null;

				//_currentSession.Start();
				_currentSession.Filter();
			}

			//if (_currentSession != null && _currentSession.SelectedCompletionSet != null)
			//{
			//	_currentSession.SelectedCompletionSet.SelectBestMatch();
			//	if (_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected)
			//	{
			//		_currentSession.SelectedCompletionSet.Filter();
			//		_currentSession.SelectedCompletionSet.SelectBestMatch();
			//	}
			//}
            return true;
        }

        bool ShouldCancel()
        {
            if (_currentSession == null) return false;

            int pos = _currentSession.TextView.Caret.Position.BufferPosition.Position - 1;
            
            if (pos < 0) return true;
            char front = _currentSession.TextView.TextSnapshot[pos];

			return !front.IsWordOrDot();
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}