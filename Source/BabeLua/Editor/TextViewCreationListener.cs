using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaLanguage;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Babe.Lua.Editor
{
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType("Lua")]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	internal sealed class TextViewCreationListener : IVsTextViewCreationListener
	{
		[Import]
		IVsEditorAdaptersFactoryService AdaptersFactory = null;

		[Import]
		ICompletionBroker CompletionBroker = null;

		CompletionCommandFilter CompletionFilter;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			var view = AdaptersFactory.GetWpfTextView(textViewAdapter);

			Debug.Assert(view != null);

			CompletionFilter = new CompletionCommandFilter(view, CompletionBroker);

			IOleCommandTarget next;
			textViewAdapter.AddCommandFilter(CompletionFilter, out next);
			CompletionFilter.Next = next;

			var filter = new CommandFilter(view);
			textViewAdapter.AddCommandFilter(filter, out next);
			filter.Next = next;
		}
	}
}
