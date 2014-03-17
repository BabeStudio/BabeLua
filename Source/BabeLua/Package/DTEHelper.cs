using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LuaLanguage.DataModel;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Babe.Lua.Editor;
using System.IO;

namespace Babe.Lua
{
    class DTEHelper
    {
        public DTE DTE { get; private set; }

        DocumentEvents docEvents;
        CommandEvents cmdEvents;
        SolutionEvents solEvents;
        WindowEvents wndEvents;
        DTEEvents dteEvents;

        public ITextSelection SelectionPage { get; set; }

        public static DTEHelper Current { get; private set; }

        public DTEHelper(DTE dte)
        {
            this.DTE = dte;
            Current = this;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //System.Windows.Input.Keyboard.AddPreviewKeyDownHandler(System.Windows.Application.Current.MainWindow, new System.Windows.Input.KeyEventHandler(App_KeyDown));

            System.Windows.Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
			
            docEvents = DTE.Events.DocumentEvents;
            //docEvents.DocumentOpening += DocumentEvents_DocumentOpening;
            //docEvents.DocumentClosing += VSPackage1Package_DocumentClosing;

            //VS关闭命令事件
            //cmdEvents = DTE.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 229];
            //cmdEvents.BeforeExecute += cmdEvents_BeforeExecute;
            
            //solEvents = DTE.Events.SolutionEvents;
            //solEvents.Opened += solEvents_Opened;
            
            wndEvents = DTE.Events.WindowEvents;
            wndEvents.WindowActivated += wndEvents_WindowActivated;

            dteEvents = DTE.Events.DTEEvents;
            dteEvents.OnStartupComplete += dteEvents_OnStartupComplete;

			TextViewCreationListener.FileContentChanged += TextViewCreationListener_FileContentChanged;

			UploadLog();
        }

		void TextViewCreationListener_FileContentChanged(object sender, Irony.Parsing.ParseTree e)
		{
			IntellisenseHelper.Refresh(e);
		}

		void UploadLog()
		{
			System.Net.NetworkInformation.PhysicalAddress addr = null;
			var ints = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
			foreach (var intf in ints)
			{
				if (intf.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
				{
					addr = intf.GetPhysicalAddress();
					break;
				}
			}
			if (addr != null)
			{
				//发送给服务器，作为用户标识符
				try
				{
					System.Net.HttpWebRequest req = System.Net.WebRequest.CreateHttp(string.Format("http://1.netchat.duapp.com/BabeLua?Address={0}", addr.ToString()));
					req.Method = "POST";
					req.ContentLength = 0;
					req.Timeout = 3000;
					req.BeginGetResponse(null, null);
				}
				catch { }
				try
				{
					string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);

					if (File.Exists(path))
					{
						StreamReader reader = new StreamReader(path, new UTF8Encoding(false));

						byte[] dat = UTF8Encoding.UTF8.GetBytes("data=" + reader.ReadToEnd());
						reader.Dispose();
							
						System.Net.HttpWebRequest req = System.Net.WebRequest.CreateHttp("http://babelua.duapp.com/");
						req.Method = "POST";
						req.ContentLength = dat.Length;
						req.ContentType = "application/x-www-form-urlencoded";
						var resstream = req.GetRequestStream();
						resstream.Write(dat, 0, dat.Length);
						resstream.Dispose();

						//req.Timeout = 3000;
						req.BeginGetResponse((ar) =>
						{
							var resp = req.EndGetResponse(ar) as System.Net.HttpWebResponse;
							byte[] buf = new byte[resp.ContentLength];
							var respstream = resp.GetResponseStream();
							respstream.Read(buf, 0, buf.Length);
							respstream.Dispose();

							if (buf.Length == 1 && buf[0] == 49)
							{
								File.Delete(path);
							}
							resp.Close();
						}, null);
					}
				}
				catch { }
			}
		}

        void App_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Diagnostics.Debug.Print(e.Key.ToString());
        }

        void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show("BabeLua has run into an error, you may have to restart visual studio.", "Error");
            
            Setting.Instance.LogError(e.Exception);
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
			System.Windows.MessageBox.Show("BabeLua has run into an error, you may have to restart visual studio.", "Error");
			e.Handled = true;
            
            Setting.Instance.LogError(e.Exception);
        }

		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			System.Windows.MessageBox.Show("BabeLua has run into an error, you may have to restart visual studio.", "Error");
			Setting.Instance.LogError(e.ExceptionObject as Exception);
		}

        public void SearchWndClosed(ISearchWnd wnd)
        {
            if (wnd != CurrentSearchWnd) return;

            if (wnd == SearchWndPane1.Current)
            {
                CurrentSearchWnd = SearchWndPane2.Current;
            }
            else if (wnd == SearchWndPane2.Current)
            {
                CurrentSearchWnd = SearchWndPane1.Current;
            }
        }

        void dteEvents_OnStartupComplete()
        {
            if (BabePackage.Setting.IsFirstRun)
            {
				var ret = System.Windows.MessageBox.Show("Do you want BabeLua tool windows open automatically ?", "BabeLua", System.Windows.MessageBoxButton.YesNo);
				if (ret == System.Windows.MessageBoxResult.Yes)
				{
					BabePackage.Current.ShowFolderWindow(null, null);
					BabePackage.Current.ShowOutlineWindow(null, null);
					BabePackage.Current.ShowSearchWindow1(null, null);
					BabePackage.Current.ShowSettingWindow(null, null);
				}
				else
				{
					System.Windows.MessageBox.Show("BabeLua tool windows would not open automatically.\r\nYou can open them with menu [LUA] -> [Views].");
				}
            }
            else if(!string.IsNullOrWhiteSpace(BabePackage.Setting.CurrentSetting))
            {
                LuaLanguage.DataModel.IntellisenseHelper.Scan();
            }

            if (BabePackage.Setting.HideUselessViews)
            {
                HiddenVSWindows();
            }

            //打开上次关闭前打开的文件
        }

        ISearchWnd CurrentSearchWnd;
        void wndEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            if (GotFocus.ObjectKind.Contains(GuidList.SearchWindowString1))
            {
                CurrentSearchWnd = SearchWndPane1.Current;
            }
            else if (GotFocus.ObjectKind.Contains(GuidList.SearchWindowString2))
            {
                CurrentSearchWnd = SearchWndPane2.Current;
            }
            if (LostFocus != null)
            {
                if (LostFocus.ObjectKind.Contains(GuidList.OutlineWindowString))
                {
                    OutlineWndPane.Current.LostFocus();
                }
                else if (LostFocus.ObjectKind.Contains(GuidList.FolderWindowString))
                {
                    FolderWndPane.Current.LostFocus();
                }
            }
        }

        public void RefreshSearchWnd(string txt, bool AllFile)
        {
            if (CurrentSearchWnd == null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(
                     () => BabePackage.Current.ShowSearchWindow1(null, null)
                    );
            }
            if (CurrentSearchWnd != null)
            {
                if (CurrentSearchWnd == SearchWndPane1.Current)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(
                     () => BabePackage.Current.ShowSearchWindow1(null, null)
                    );
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(
                     () => BabePackage.Current.ShowSearchWindow2(null, null)
                    );
                }
                CurrentSearchWnd.Search(txt, AllFile);
            }
        }

        public void RefreshOutlineWnd()
        {
            if (OutlineWndPane.Current != null) OutlineWndPane.Current.Refresh();
        }

        public void RefreshFolderWnd()
        {
            if (FolderWndPane.Current != null) FolderWndPane.Current.Refresh();
        }

        public void RefreshEditorOutline()
        {
            if (EditorMarginProvider.CurrentMargin != null)
            {
                EditorMarginProvider.CurrentMargin.Refresh();
            }
        }

        public void UpdateUI()
        {
            RefreshFolderWnd();
            RefreshOutlineWnd();
            RefreshEditorOutline();
        }

        void solEvents_Opened()
        {
            var projects = DTE.Solution.Projects;
        }

        void cmdEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            //var Files = new System.Xml.Linq.XElement(BabePackage.Setting.OpenFiles.Name);
            
            //foreach (Document doc in DTE.Documents)
            //{
            //    if (doc != DTE.ActiveDocument)
            //    {
            //        var file = new System.Xml.Linq.XElement("File");
            //        file.Value = doc.FullName;
            //        Files.Add(file);
            //    }
            //}

            //if (DTE.ActiveDocument != null)
            //{
            //    var file = new System.Xml.Linq.XElement("Active");
            //    file.Value = DTE.ActiveDocument.FullName;
            //    file.Add(new 
            //}
        }

        void SolutionEvents_Opened()
        {
            Debug.Print("project opened");
        }

        void VSPackage1Package_DocumentClosing(Document Document)
        {
            Debug.Print("document closed");
        }

        void DocumentEvents_DocumentOpening(string DocumentPath, bool ReadOnly)
        {
            //LuaLanguage.Helper.IntellisenseHelper.SetFile(DocumentPath);
        }

        public void ExecuteCmd(string cmd, string args = null)
        {
            if (DTE == null) return;
            DTE.ExecuteCommand(cmd, args);
        }

        public void SetStatusBarText(string text)
        {
            DTE.StatusBar.Text = text;
        }

        public StatusBar GetStatusBar()
        {
            return DTE.StatusBar;
        }

        public void FindSelectTokenRef(bool AllFiles)
        {
            if (SelectionPage == null) return;
            var spans = SelectionPage.SelectedSpans;
            if (spans.Count > 1) return;
            var txt = SelectionPage.TextView.TextSnapshot.GetText(spans[0]);
            if (string.IsNullOrWhiteSpace(txt)) return;

            //HighlightCurrentPosition();
			if (!txt.All(ch => { return ch.IsWord(); })) return;

            if (BabePackage.Setting.ContainsSearchFilter(txt)) return;
			
			HighlightPosition(spans[0].Start, spans[0].Length);

            RefreshSearchWnd(txt, AllFiles);
        }

        public void GoTo(int line = 0, int column = 0, int length = 0)
        {
            Guid logicalView = VSConstants.LOGVIEWID.Primary_guid;

            var buffer = SelectionPage.TextView.TextBuffer as IVsTextBuffer;

            IVsTextManager mgr = BabePackage.Current.GetService(typeof(VsTextManagerClass)) as IVsTextManager;


            mgr.NavigateToLineAndColumn(buffer, ref logicalView, line, column, line, column + length);
        }

        public void GoTo(string file, int line = 0, int column = 0, int length = 0, bool highlight = false)
        {
            PreviewDocument(file);

            while (DTE.ActiveDocument.FullName != file) ;
            if (line == 0 && column == 0) return;

            GoTo(line, column, length);

            MarkLine(line);

			if (highlight)
				//HighlightCurrentPosition();
				HighlightPosition(line, column, length);
        }

        public void OpenDocument(string file)
        {
            DTE.ItemOperations.OpenFile(file, EnvDTE.Constants.vsViewKindPrimary);
        }

        public void PreviewDocument(string file)
        {
            using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, Microsoft.VisualStudio.VSConstants.NewDocumentStateReason.Navigation))
            {
                DTE.ItemOperations.OpenFile(file, EnvDTE.Constants.vsViewKindPrimary);
            }
        }

        public void MarkLine(int line)
        {
            Editor.MarkPosTaggerProvider.CurrentTagger.ShowTag(line);
        }

        public void HighlightCurrentPosition()
        {
            TextMarker.FindWordTaggerProvider.CurrentTagger.UpdateAtCaretPosition(SelectionPage.TextView.Caret.Position);
        }

		public void HighlightPosition(int position, int length)
		{
			TextMarker.FindWordTaggerProvider.CurrentTagger.UpdateAtPosition(position, length);
		}

		public void HighlightPosition(int line, int column, int length)
		{
			TextMarker.FindWordTaggerProvider.CurrentTagger.UpdateAtPosition(line, column, length);
		}

        public void ShowEditorOutlineMarginLeft()
        {
            EditorMarginProvider.CurrentMargin.OpenLeftOutline();
        }

        public void ShowEditorOutlineMarginRight()
        {
            EditorMarginProvider.CurrentMargin.OpenRightOutline();
        }

        public void HiddenVSWindow(string vsWindowsKind)
        {
            Window window = DTE.Windows.Item(vsWindowsKind);
            if (window != null)
            {
                window.Visible = false;
            }
        }

        public void HiddenVSWindows()
        {
            HiddenVSWindow(EnvDTE.Constants.vsext_wk_SProjectWindow);       //显示解决方案及其项目的“项目”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindFindResults1);      //“查找结果 1”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindFindResults2);      //“查找结果 2”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindClassView);         //“类视图”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindCommandWindow);     //“命令”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindDocumentOutline);   //“文档大纲”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindDynamicHelp);       //“动态帮助”窗口
            //            HiddenVSWindow(EnvDTE.Constants.vsWindowKindMacroExplorer);     //“Macro 资源管理器”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindObjectBrowser);     //“对象浏览器”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindOutput);            //“输出”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindProperties);        //“属性”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindResourceView);      //“资源编辑器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindServerExplorer);    //“服务器资源管理器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindSolutionExplorer);  //“解决方案资源管理器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindTaskList);          //“任务列表”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindToolbox);           //“工具箱”
        }
    }
}
