using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.CommandBars;
using Babe.Lua.Editor;

namespace Babe.Lua
{
    #region Properties
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(SearchWndPane1),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.Outputwindow
        )]
    [ProvideToolWindow(typeof(SearchWndPane2),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.Outputwindow
        )]
    [ProvideToolWindow(typeof(OutlineWndPane),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.SolutionExplorer
        )]
    [ProvideToolWindow(typeof(FolderWndPane),
        Style=VsDockStyle.Linked,
        Orientation=ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.StartPage
        )]
    [ProvideToolWindow(typeof(SettingWndPane),
        Style = VsDockStyle.Tabbed,
        Orientation = ToolWindowOrientation.none,
        Window = ToolWindowGuids80.StartPage
        )]
    [ProvideToolWindowVisibility(typeof(FolderWndPane), "f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideToolWindowVisibility(typeof(OutlineWndPane), "f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    //[ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [Guid(GuidList.PkgString)]
    #endregion
    public sealed class BabePackage : Package
    {
        DTEHelper events;
        OleComponent OleCom;
        public static BabePackage Current;
        internal static Setting Setting = Setting.Instance;

        public BabePackage()
        {
			Current = this;
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            events = new DTEHelper(GetService(typeof(DTE)) as DTE);

            RegisterOleComponent();

            AddCommandBars();

            if (Setting.HideUselessViews)
            {
                HiddenToolBars();
            }
        }

        #region Menu Events Handler
        public void ShowSearchWindow1(object sender, EventArgs e)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(SearchWndPane1), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper,new WindowFrameSink(SearchWndPane1.Current)));
            
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowSearchWindow2(object sender, EventArgs e)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(SearchWndPane2), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, new WindowFrameSink(SearchWndPane2.Current)));

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowOutlineWindow(object sender, EventArgs e)
        {
			ToolWindowPane window = this.FindToolWindow(typeof(OutlineWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            
            //windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Dock);

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowFolderWindow(object sender, EventArgs e)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(FolderWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowSettingWindow(object sender, EventArgs e)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(SettingWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void RunLuaExecutable(object sender, EventArgs e)
        {
            var set = CurrentSetting;

            if (CurrentSetting == null || string.IsNullOrWhiteSpace(CurrentSetting.LuaExecutable))
            {
                System.Windows.MessageBox.Show(Properties.Resources.GuideLuaExecutablePath);
                ShowSettingWindow(this, null);
                if (SettingWndPane.Current != null)
                {
                    SettingWndPane.Current.ShowToExecutable();
                }
            }
            else
            {
                try
                {
                    var pro = new System.Diagnostics.Process();
                    pro.StartInfo.FileName = CurrentSetting.LuaExecutable;
                    pro.StartInfo.Arguments = CurrentSetting.CommandLine;
                    pro.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(CurrentSetting.LuaExecutable);
                    pro.Start();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format(Properties.Resources.LuaExecutableError, ex.Message));
                }
            }
        }

        public void ShotKeyHandler(object sender, EventArgs e)
        {
            var cmd = sender as MenuCommand;
            if (cmd != null)
            {
                switch ((uint)cmd.CommandID.ID)
                {
                    case PkgCmdIDList.ShotKey0:
                    case PkgCmdIDList.ShotKey1:
                    case PkgCmdIDList.ShotKey2:
                    case PkgCmdIDList.ShotKey3:
                    case PkgCmdIDList.ShotKey4:
                    case PkgCmdIDList.ShotKey5:
                    case PkgCmdIDList.ShotKey6:
                    case PkgCmdIDList.ShotKey7:
                    case PkgCmdIDList.ShotKey8:
                    case PkgCmdIDList.ShotKey9:
                        {
                            int index = cmd.CommandID.ID-(int)PkgCmdIDList.ShotKey0;
                            string keyName = BabePackage.Setting.GetKeyName(index);
                            if (keyName == BabePackage.Setting.GetKeyBindingName("FolderExplorer"))
                            {
                                ShowFolderWindow(this, null);
                                if (FolderWndPane.Current != null)
                                {
                                    FolderWndPane.Current.SetFocus();
                                }
                            }
                            else if (keyName == BabePackage.Setting.GetKeyBindingName("Outline"))
                            {
                                ShowOutlineWindow(this, null);
                                if (OutlineWndPane.Current != null)
                                {
                                    OutlineWndPane.Current.SetFocus();
                                }
                            }
                            else if (keyName == BabePackage.Setting.GetKeyBindingName("OutlineMarginLeft"))
                            {
                                if (DTEHelper.Current.SelectionPage != null)
                                {
                                    DTEHelper.Current.ShowEditorOutlineMarginLeft();
                                }
                            }
                            else if (keyName == BabePackage.Setting.GetKeyBindingName("OutlineMarginRight"))
                            {
                                if (DTEHelper.Current.SelectionPage != null)
                                {
                                    DTEHelper.Current.ShowEditorOutlineMarginRight();
                                }
                            }
                            else if (keyName == BabePackage.Setting.GetKeyBindingName("RunLuaExe"))
                            {
                                RunLuaExecutable(null, null);
                            }
                        }
                        break;
/*                    case PkgCmdIDList.ShotKey1:
                        System.Diagnostics.Debug.Print("ShotKey1");
                        ShowFolderWindow(this, null);
                        if (FolderWndPane.Current != null)
                        {
                            FolderWndPane.Current.SetFocus();
                        }
                        break;
                    case PkgCmdIDList.ShotKey2:
                        System.Diagnostics.Debug.Print("ShotKey2");
                        ShowOutlineWindow(this, null);
                        if (OutlineWndPane.Current != null)
                        {
                            OutlineWndPane.Current.SetFocus();
                        }
                        break;
                    case PkgCmdIDList.ShotKey3:
                        System.Diagnostics.Debug.Print("ShotKey3");
                        if (DTEHelper.Current.SelectionPage != null)
                        {
                            DTEHelper.Current.ShowEditorOutlineMarginLeft();
                        }
                        break;
                    case PkgCmdIDList.ShotKey4:
                        System.Diagnostics.Debug.Print("ShotKey4");
                        if (DTEHelper.Current.SelectionPage != null)
                        {
                            DTEHelper.Current.ShowEditorOutlineMarginRight();
                        }
                        break;
                    case PkgCmdIDList.ShotKey5:
                        System.Diagnostics.Debug.Print("ShotKey5");

                        break;
                    case PkgCmdIDList.ShotKey6:
                        System.Diagnostics.Debug.Print("ShotKey6");

                        break;*/
                    default:
                        break;
/*                    case PkgCmdIDList.ShotKey7:
                        System.Diagnostics.Debug.Print("ShotKey7");

                        break;
                    case PkgCmdIDList.ShotKey8:
                        System.Diagnostics.Debug.Print("ShotKey8");

                        break;
                    case PkgCmdIDList.ShotKey9:
                        System.Diagnostics.Debug.Print("ShotKey9");

                        break;
                    case PkgCmdIDList.ShotKey0:
                        System.Diagnostics.Debug.Print("ShotKey0");

                        break;*/
                }
            }
        }
        #endregion

        private void RegisterOleComponent()
        {
            OleCom = new OleComponent();

            var ocm = this.GetService(typeof(SOleComponentManager)) as IOleComponentManager;

            if (ocm != null)
            {
                uint pwdID;
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));

                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedAllActiveNotifs;
                ocm.FRegisterComponent(OleCom, crinfo, out pwdID);
            }
        }

        void HiddenToolBars()
        {
            var cmdBars = (CommandBars)DTEHelper.Current.DTE.CommandBars;

            foreach (CommandBar bar in cmdBars)
            {
                if (bar.Type != MsoBarType.msoBarTypeMenuBar)
                {
                    bar.Enabled = false;
                }
            }
            
            var menu = cmdBars.ActiveMenuBar;
            foreach (CommandBarControl bar in menu.Controls)
            {
                bar.Visible = false;
            }

            menu.Controls["File"].Visible = true;
            menu.Controls["Edit"].Visible = true;
            menu.Controls["Lua"].Visible = true;
            menu.Controls["Window"].Visible = true;
            menu.Controls["Help"].Visible = true;
            menu.Controls["Tools"].Visible = true;

            var file = menu.Controls["File"] as CommandBarPopup;
            foreach (CommandBarControl btn in file.Controls)
            {
                btn.Visible = false;
            }
            file.Controls["Close"].Visible = true;
            file.Controls["Recent Files"].Visible = true;
            file.Controls["Exit"].Visible = true;
        }

        void AddCommandBars()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                CommandID Search1CmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SearchWindow1);
                MenuCommand menuSearch1Win = new MenuCommand(ShowSearchWindow1, Search1CmdID);
                mcs.AddCommand(menuSearch1Win);

                CommandID Search2CmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SearchWindow2);
                MenuCommand menuSearch2Win = new MenuCommand(ShowSearchWindow2, Search2CmdID);
                mcs.AddCommand(menuSearch2Win);

                CommandID outlineCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.OutlineWindow);
                MenuCommand menuOutlineWin = new MenuCommand(ShowOutlineWindow, outlineCmdID);
                mcs.AddCommand(menuOutlineWin);

                CommandID folderCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.FolderWindow);
                MenuCommand menuFolderWin = new MenuCommand(ShowFolderWindow, folderCmdID);
                mcs.AddCommand(menuFolderWin);

                CommandID settingCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SettingWindow);
                MenuCommand menuSettingWin = new MenuCommand(ShowSettingWindow, settingCmdID);
                mcs.AddCommand(menuSettingWin);

                CommandID runCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.RunLuaExecutable);
                MenuCommand menuRun = new MenuCommand(RunLuaExecutable, runCmdID);
                mcs.AddCommand(menuRun);

                CommandID ShotKey1 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey1);
                MenuCommand MenuShotKey1 = new MenuCommand(ShotKeyHandler, ShotKey1);
                mcs.AddCommand(MenuShotKey1);

                CommandID ShotKey2 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey2);
                MenuCommand MenuShotKey2 = new MenuCommand(ShotKeyHandler, ShotKey2);
                mcs.AddCommand(MenuShotKey2);

                CommandID ShotKey3 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey3);
                MenuCommand MenuShotKey3 = new MenuCommand(ShotKeyHandler, ShotKey3);
                mcs.AddCommand(MenuShotKey3);

                CommandID ShotKey4 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey4);
                MenuCommand MenuShotKey4 = new MenuCommand(ShotKeyHandler, ShotKey4);
                mcs.AddCommand(MenuShotKey4);

                CommandID ShotKey5 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey5);
                MenuCommand MenuShotKey5 = new MenuCommand(ShotKeyHandler, ShotKey5);
                mcs.AddCommand(MenuShotKey5);

                CommandID ShotKey6 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey6);
                MenuCommand MenuShotKey6 = new MenuCommand(ShotKeyHandler, ShotKey6);
                mcs.AddCommand(MenuShotKey6);

                CommandID ShotKey7 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey7);
                MenuCommand MenuShotKey7 = new MenuCommand(ShotKeyHandler, ShotKey7);
                mcs.AddCommand(MenuShotKey7);

                CommandID ShotKey8 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey8);
                MenuCommand MenuShotKey8 = new MenuCommand(ShotKeyHandler, ShotKey8);
                mcs.AddCommand(MenuShotKey8);

                CommandID ShotKey9 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey9);
                MenuCommand MenuShotKey9 = new MenuCommand(ShotKeyHandler, ShotKey9);
                mcs.AddCommand(MenuShotKey9);

                CommandID ShotKey0 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey0);
                MenuCommand MenuShotKey0 = new MenuCommand(ShotKeyHandler, ShotKey0);
                mcs.AddCommand(MenuShotKey0);
            }
        }

        public int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            throw new NotImplementedException();
        }

        public int Close()
        {
            throw new NotImplementedException();

        }

        public new object GetService(Type service)
        {
            return base.GetService(service);
        }

        public LuaSet CurrentSetting
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Setting.CurrentSetting)) return null;

                return Setting.GetSetting(Setting.CurrentSetting);
            }
        }
    }
    public class KeyBindingSet
    {
        public string key;

        public KeyBindingSet() { }

        public KeyBindingSet(string key)
        {
            this.key = key;
        }
    }
    public class LuaSet
    {
		public string Name;
        public string Folder;
        public string LuaExecutable;
        public string CommandLine;
		public EncodingName Encoding;

        public LuaSet() { }

		public LuaSet(string LuaPath, string LuaExecutable, string CommandLine, EncodingName Encoding)
		{
			this.Folder = LuaPath;
			this.LuaExecutable = LuaExecutable;
			this.CommandLine = CommandLine;
			this.Encoding = Encoding;
		}

        public LuaSet(string Name, string LuaPath, string LuaExecutable = null, string CommandLine = null, EncodingName Encoding = EncodingName.UTF8):this(LuaPath, LuaExecutable, CommandLine, Encoding)
        {
			this.Name = Name;
        }
    }
}
