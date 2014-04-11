using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua;
using Babe.Lua.Package;
using Babe.Lua.DataModel;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;

namespace Babe.Lua.Editor
{
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType("Lua")]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	internal sealed class TextViewCreationListener : IVsTextViewCreationListener , IDisposable
	{
		[Import]
		public IVsEditorAdaptersFactoryService AdaptersFactory { get; private set; }

		[Import]
		public ICompletionBroker CompletionBroker { get; private set; }

		public static event EventHandler<FileContentChangedEventArgs> FileContentChanged;

		Babe.Lua.Intellisense.CompletionCommandFilter CompletionFilter;

		System.Threading.Timer DelayRefreshTimer;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			System.Diagnostics.Debug.Print("document create");

			var view = AdaptersFactory.GetWpfTextView(textViewAdapter);

			Debug.Assert(view != null);

			view.GotAggregateFocus += view_GotAggregateFocus;
			view.Closed += view_Closed;
			view.TextBuffer.Changed += TextBuffer_Changed;

			CompletionFilter = new Intellisense.CompletionCommandFilter(view, CompletionBroker);

			IOleCommandTarget next;
			textViewAdapter.AddCommandFilter(CompletionFilter, out next);
			CompletionFilter.Next = next;

			//var filter = new CommandFilter(view);
			//textViewAdapter.AddCommandFilter(filter, out next);
			//filter.Next = next;

			EditorReport.TextViewCreated(view);
		}

		void TextBuffer_Changed(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
		{
			if (_cur == null || e.After != _cur.TextBuffer.CurrentSnapshot) return;
			if (DelayRefreshTimer != null)
			{
				DelayRefreshTimer.Dispose();
			}
			DelayRefreshTimer = new System.Threading.Timer(RefreshFile, null, 500, System.Threading.Timeout.Infinite);
		}

		void RefreshFile(object arg)
		{
			Irony.Parsing.Parser parser = new Irony.Parsing.Parser(Grammar.LuaGrammar.Instance);
			var tree = parser.Parse(_cur.TextBuffer.CurrentSnapshot.GetText());
			OnFileContentChanged(_cur.TextBuffer.CurrentSnapshot, tree);
		}

		void view_Closed(object sender, EventArgs e)
		{
			var view = sender as IWpfTextView;

			view.GotAggregateFocus -= view_GotAggregateFocus;

			System.Diagnostics.Debug.Print("document close");
		}

		void view_GotAggregateFocus(object sender, EventArgs e)
		{
			if (_cur != sender)
			{
				var file = DTEHelper.Current.DTE.ActiveDocument.FullName;
				if (!System.IO.File.Exists(file))
				{
					//文件已经被移除，我们关闭窗口
					IntellisenseHelper.RemoveFile(file);
					DTEHelper.Current.DTE.ActiveDocument.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
					return;
				}

				System.Diagnostics.Debug.Print("document got focus");
				_cur = sender as IWpfTextView;

				DTEHelper.Current.SelectionPage = _cur.Selection;

				IntellisenseHelper.SetCurrentFile(file);

				DTEHelper.Current.SetStatusBarText(EncodingDecide.DecideFileEncoding(DTEHelper.Current.DTE.ActiveDocument.FullName).ToString());
			}
		}

		void OnFileContentChanged(ITextSnapshot snapshot, Irony.Parsing.ParseTree tree)
		{
			if (FileContentChanged != null)
			{
				FileContentChanged(this, new FileContentChangedEventArgs(snapshot,tree));
			}
		}

		static IWpfTextView _cur;

		public void Dispose()
		{
			if (DelayRefreshTimer != null)
			{
				DelayRefreshTimer.Dispose();
			}
		}
	}

	class FileContentChangedEventArgs : EventArgs
	{
		public Irony.Parsing.ParseTree Tree { get; private set; }
		public ITextSnapshot Snapshot { get; private set; }

		public FileContentChangedEventArgs(ITextSnapshot Snapshot, Irony.Parsing.ParseTree Tree)
		{
			this.Tree = Tree;
			this.Snapshot = Snapshot;
		}
	}

	static class EditorReport
	{
		static bool _hasSendReport = false;
		static int count = 0;
		const int max = 50;

		static void TextBuffer_Changed(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
		{
			if (++count > max)
			{
				var buf = sender as ITextBuffer;
				if (buf != null) buf.Changed -= TextBuffer_Changed;
				if (!_hasSendReport)
				{
					DTEHelper.Current.UpdateUserData("edit");
					_hasSendReport = true;
				}
			}
		}

		public static void TextViewCreated(IWpfTextView view)
		{
			if (!_hasSendReport)
			{
				view.TextBuffer.Changed += TextBuffer_Changed;
				count++;
			}
		}
	}
}
