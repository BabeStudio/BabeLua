using Babe.Lua.DataModel;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Babe.Lua.Editor
{
    /// <summary>
    /// OutlineMarginControl.xaml 的交互逻辑
	/// 目前OutlineMargin还承担了显示编码、监测搜索触发等功能，后续考虑分离出去
    /// </summary>
    public partial class OutlineMarginControl : UserControl
    {
        IWpfTextViewHost TextViewHost;

        public OutlineMarginControl(IWpfTextViewHost TextViewHost)
        {
            InitializeComponent();

            this.TextViewHost = TextViewHost;

            this.Loaded += OutlineMarginControl_Loaded;
            this.Unloaded += OutlineMarginControl_Unloaded;
            this.KeyDown += OutlineMarginControl_KeyDown;
        }

        void OutlineMarginControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                TextViewHost.TextView.VisualElement.Focus();
            }
        }

        public void Refresh()
        {
            var File = FileManager.Instance.CurrentFile;

            if (File == null)
            {
                ComboBox_Table.ItemsSource = null;
                ComboBox_Member.ItemsSource = null;
            }
            else if (ComboBox_Table.ItemsSource != File.Members)
            {
                ComboBox_Table.ItemsSource = File.Members;

                var list = new List<LuaMember>();
                foreach (var member in File.Members)
                {
                    if (member is LuaTable)
                    {
                        list.AddRange((member as LuaTable).Members);
                    }
                    else
                    {
                        list.Add(member);
                    }
                }
                ComboBox_Member.ItemsSource = list;
            }
        }

        void OutlineMarginControl_Unloaded(object sender, RoutedEventArgs e)
        {
            TextViewHost.HostControl.MouseDoubleClick -= HostControl_MouseDoubleClick;
        }

        void OutlineMarginControl_Loaded(object sender, RoutedEventArgs e)
        {
			//System.Diagnostics.Debug.Print("document load");
			//var file = DTEHelper.Current.DTE.ActiveDocument.FullName;
			//if (!System.IO.File.Exists(file))
			//{
			//	//文件已经被移除，我们关闭窗口
			//	IntellisenseHelper.RemoveFile(file);
			//	DTEHelper.Current.DTE.ActiveDocument.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
			//	return;
			//}

			//DTEHelper.Current.SelectionPage = TextViewHost.TextView.Selection;

			//IntellisenseHelper.SetFile(file);

            TextViewHost.HostControl.MouseDoubleClick += HostControl_MouseDoubleClick;

            Refresh();
            //DTEHelper.Current.SetStatusBarText(EncodingDecide.DecideFileEncoding(DTEHelper.Current.DTE.ActiveDocument.FullName).ToString());

            //DTEHelper.Current.DTE.ActiveDocument.ActiveWindow.Caption += string.Format("({0})", EncodingDecide.DecideFileEncoding(DTEHelper.Current.DTE.ActiveDocument.FullName).ToString());
        }

        private void HostControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (BabePackage.Setting.IsFirstRun)
            {
                BabePackage.Setting.IsFirstRun = false;
                MessageBox.Show("press <Ctrl> key to search words in current file\r\npress <Alt> key to search in all files", "Tips");
                DTEHelper.Current.FindSelectTokenRef(false);
                DTEHelper.Current.OpenDocument(DTEHelper.Current.DTE.ActiveDocument.FullName);
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                DTEHelper.Current.FindSelectTokenRef(false);
                DTEHelper.Current.OpenDocument(DTEHelper.Current.DTE.ActiveDocument.FullName);
            }
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                DTEHelper.Current.FindSelectTokenRef(true);
                DTEHelper.Current.OpenDocument(DTEHelper.Current.DTE.ActiveDocument.FullName);
            }
        }

        private void Combo_Table_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Table.SelectedItem != null)
            {
                var tb = ComboBox_Table.SelectedItem as LuaMember;

                if(tb is LuaTable) ComboBox_Member.ItemsSource = (tb as LuaTable).Members;

                DTEHelper.Current.GoTo(tb.Line);
            }
        }

        private void Combo_Member_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Member.SelectedItem != null)
            {
                var member = ComboBox_Member.SelectedItem as LuaMember;

                DTEHelper.Current.GoTo(member.Line);
            }
        }
    }
}
