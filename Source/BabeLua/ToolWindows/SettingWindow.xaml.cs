using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows.Input;
using System;
using System.Windows.Forms;
using Babe.Lua.DataModel;
using Babe.Lua.Editor;
using Babe.Lua.Package;

namespace Babe.Lua.ToolWindows
{
    public partial class SettingWindow : System.Windows.Controls.UserControl
    {
        public SettingWindow()
        {
            InitializeComponent();
            CheckBox_HideView.IsChecked = BabePackage.Setting.HideUselessViews;

            this.Loaded += (s, e) =>
            {
                InitComboBox();
                //InitKeyWordsList();
                //InitTextBoxXml();
                

                if (!string.IsNullOrWhiteSpace(BabePackage.Setting.CurrentSetting))
                {
                    TextBlock_Select.Text = BabePackage.Setting.CurrentSetting;

                    ComboBox_Settings.SelectedItem = BabePackage.Setting.CurrentSetting;

                    ReadSetting(BabePackage.Setting.CurrentSetting);
                }
            };

            ComboBox_FolderExplorer.ItemsSource = BabePackage.Setting.AllBindingKey;
            ComboBox_Outline.ItemsSource = BabePackage.Setting.AllBindingKey;
            ComboBox_OutlineMarginLeft.ItemsSource = BabePackage.Setting.AllBindingKey;
            ComboBox_OutlineMarginRight.ItemsSource = BabePackage.Setting.AllBindingKey;
            ComboBox_RunLuaExe.ItemsSource = BabePackage.Setting.AllBindingKey;

            var keyFolderExplorer = BabePackage.Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindFolder);
            var keyOutline = BabePackage.Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindOutline);
            var keyOutlineMarginLeft = BabePackage.Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindEditorOutlineLeft);
            var keyOutlineMarginRight = BabePackage.Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindEditorOutlineRight);
            var keyRunLuaExe = BabePackage.Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindRunExec);
            ComboBox_FolderExplorer.SelectedItem = keyFolderExplorer;
            ComboBox_Outline.SelectedItem = keyOutline;
            ComboBox_OutlineMarginLeft.SelectedItem = keyOutlineMarginLeft;
            ComboBox_OutlineMarginRight.SelectedItem = keyOutlineMarginRight;
            ComboBox_RunLuaExe.SelectedItem = keyRunLuaExe;
        }

        private void InitTextBoxXml()
        {
//            TextBox_Xml.Text = BabePackage.Setting.SearchFilters.ToString();
        }

        void ReadSetting(string name)
        {
            var CurSet = BabePackage.Setting.GetSetting(name);

            if (CurSet == null) return;

            TextBox_LuaPath.Text = CurSet.Folder;
            TextBox_LuaExecutablePath.Text = CurSet.LuaExecutable;
            TextBox_CommandLine.Text = CurSet.CommandLine;
            TextBox_SettingName.Text = name;
			ComboBox_FileEncoding.SelectedItem = CurSet.Encoding;
        }

        //void InitKeyWordsList()
        //{
        //    var keywords = BabePackage.Setting.AllKeywords();

        //    KeywordsCount = keywords.Count;

        //    ListBox_KeyWords.ItemsSource = keywords;
        //}

        void InitComboBox()
        {
            ComboBox_Settings.ItemsSource = BabePackage.Setting.AllSetting;
        }

        private void Button_LuaPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dia = new FolderBrowserDialog();
            if (string.IsNullOrWhiteSpace(TextBox_LuaPath.Text))
            {
                dia.RootFolder = Environment.SpecialFolder.Desktop;
            }
            else
            {
                dia.SelectedPath = TextBox_LuaPath.Text;
            }

            if (dia.ShowDialog() == DialogResult.OK)
            {
                TextBox_LuaPath.Text = dia.SelectedPath;
                int iLastIndexofSeparator = dia.SelectedPath.LastIndexOf(Path.DirectorySeparatorChar);
                if (iLastIndexofSeparator >= 0)
                {
                    var projectFolder = dia.SelectedPath.Substring(0, iLastIndexofSeparator);
                    iLastIndexofSeparator = projectFolder.LastIndexOf(Path.DirectorySeparatorChar);
                    if (iLastIndexofSeparator >= 0)
                    {
                        projectFolder = projectFolder.Substring(0, iLastIndexofSeparator);

                        //set win32.exe path
                        var win32Folder = projectFolder;
                        win32Folder = Path.Combine(win32Folder, "Win32");
                        win32Folder = Path.Combine(win32Folder, "win32.exe");
                        if (File.Exists(win32Folder))
                        {
                            TextBox_LuaExecutablePath.Text = win32Folder;
                        }

                        //set project name
                        iLastIndexofSeparator = projectFolder.LastIndexOf(Path.DirectorySeparatorChar);
                        if (iLastIndexofSeparator >= 0)
                        {
                            TextBox_SettingName.Text = projectFolder.Substring(iLastIndexofSeparator + 1, projectFolder.Length - iLastIndexofSeparator - 1);
                        }
                    }
                }
            }
        }

        private void Button_LuaExecutablePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dia = new OpenFileDialog();
            string strLuaExecutablePath = TextBox_LuaExecutablePath.Text.Trim();
            if (!string.IsNullOrWhiteSpace(strLuaExecutablePath))
            {
                dia.InitialDirectory = Path.GetDirectoryName(strLuaExecutablePath);
                dia.FileName = Path.GetFileName(strLuaExecutablePath);
            }
            else
            {
                dia.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            dia.Filter = "Application|*.exe|All File|*.*";
            if (dia.ShowDialog() == DialogResult.OK)
            {
                TextBox_LuaExecutablePath.Text = dia.FileName;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var name = TextBox_SettingName.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                System.Windows.MessageBox.Show(Properties.Resources.LoseSetName);
                TextBox_SettingName.Focus();
                return;
            }
            else if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                string strConfirm = string.Format("\"{0}\" is a invalid name, please rename.", name);
                System.Windows.MessageBox.Show(strConfirm, "Error");
                TextBox_SettingName.Focus();
                TextBox_SettingName.SelectAll();
                return;
            }

            var luapath = TextBox_LuaPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(luapath) || !Directory.Exists(luapath))
            {
                System.Windows.MessageBox.Show(Properties.Resources.InvalidLuaPath);
                TextBox_LuaPath.Focus();
                TextBox_LuaPath.SelectAll();
                return;
            }

            if (BabePackage.Setting.ContainsSetting(name))
            {
                string strReplaceConfirm = string.Format("Setting \"{0}\" already exists, replace setting?", name);
                if (System.Windows.MessageBox.Show(strReplaceConfirm, "replace", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }
                BabePackage.Setting.AddSetting(name, luapath, TextBox_LuaExecutablePath.Text.Trim(), TextBox_CommandLine.Text.Trim(), (EncodingName)ComboBox_FileEncoding.SelectedItem);
                BabePackage.Setting.Save();

                //如果保存的是当前选择工程且目录发生变化则重新加载
                if (name == BabePackage.Setting.CurrentSetting && luapath != BabePackage.Setting.GetSetting(name).Folder)
                {
                    IntellisenseHelper.Scan();
                    DTEHelper.Current.UpdateUI();
                }
            }
            else
            {
				BabePackage.Setting.AddSetting(name, luapath, TextBox_LuaExecutablePath.Text.Trim(), TextBox_CommandLine.Text.Trim(), (EncodingName)ComboBox_FileEncoding.SelectedItem);
                BabePackage.Setting.Save();
            }

            InitComboBox();

            //当前选择项为空时，则选择保存的项
            if (ComboBox_Settings.SelectedItem == null || string.IsNullOrWhiteSpace(ComboBox_Settings.SelectedItem.ToString()))
            {
                ComboBox_Settings.SelectedItem = name;
            }

            Button_Save.IsEnabled = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Button_Save.IsEnabled = true;
        }

        private void Button_Select_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_Settings.SelectedItem != null)
            {
                var name = ComboBox_Settings.SelectedItem.ToString();
                if (BabePackage.Setting.ContainsSetting(name) && BabePackage.Setting.CurrentSetting != name)
                {
                    BabePackage.Setting.CurrentSetting = name;
                    TextBlock_Select.Text = BabePackage.Setting.CurrentSetting;

                    BabePackage.Setting.Save();

                    IntellisenseHelper.Scan();

                    DTEHelper.Current.UpdateUI();

                    Button_Select.IsEnabled = false;
                }
            }
        }

        private void Button_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_Settings.SelectedItem == null) return;

            var name = ComboBox_Settings.SelectedItem.ToString();

            string strDeleteConfirm = string.Format("Sure delete {0}?", name);
            if (System.Windows.MessageBox.Show(strDeleteConfirm, "delete", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }
            if (name == BabePackage.Setting.CurrentSetting)
            {
                BabePackage.Setting.CurrentSetting = string.Empty;
                IntellisenseHelper.Scan();
                DTEHelper.Current.UpdateUI();
            }

            BabePackage.Setting.RemoveSetting(name);
            BabePackage.Setting.Save();

            //ComboBox_Settings.Items.Remove(name);
            InitComboBox();

            //删除后如果当前选择项为空则默认选中一选项,否则选中当前项
            if (string.IsNullOrWhiteSpace(BabePackage.Setting.CurrentSetting))
            {
                ComboBox_Settings.SelectedItem = BabePackage.Setting.GetFirstSettingName();
            }
            else
            {
                ComboBox_Settings.SelectedItem = BabePackage.Setting.CurrentSetting;
            }

            TextBlock_Select.Text = BabePackage.Setting.CurrentSetting;

            Button_Save.IsEnabled = true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_Settings.SelectedItem != null)
            {
                var name = ComboBox_Settings.SelectedItem.ToString();
                if (BabePackage.Setting.CurrentSetting != name)
                {
                    Button_Select.IsEnabled = true;
                }
                else
                {
                    Button_Select.IsEnabled = false;
                }
                Button_Delete.IsEnabled = true;
                ReadSetting(name);
            }
            else
            {
                Button_Select.IsEnabled = false;
                Button_Delete.IsEnabled = false;
            }
        }

        private void TextBox_LuaPath_LostFocus(object sender, RoutedEventArgs e)
        {
            string path = TextBox_LuaPath.Text.Trim();

            if (!Directory.Exists(path))
            {
                System.Windows.MessageBox.Show(Properties.Resources.InvalidLuaPath);
                TextBox_LuaPath.Focus();
                TextBox_LuaPath.SelectAll();
            }
        }

        private void Button_SaveXml_Click(object sender, RoutedEventArgs e)
        {
//            BabePackage.Setting.SetSearchFilters(TextBox_Xml.Text);
        }

        private void CheckBox_HideView_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded) return;
            BabePackage.Setting.HideUselessViews = CheckBox_HideView.IsChecked.Value;
            BabePackage.Setting.Save();
        }
   
        private void Button_SaveKeyBinding_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_FolderExplorer.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Must select one key binding");
                ComboBox_FolderExplorer.Focus();
                return;
            }
            if (ComboBox_Outline.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Must select one key binding");
                ComboBox_Outline.Focus();
                return;
            }
            if (ComboBox_OutlineMarginRight.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Must select one key binding");
                ComboBox_OutlineMarginRight.Focus();
                return;
            }
            if (ComboBox_RunLuaExe.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Must select one key binding");
                ComboBox_RunLuaExe.Focus();
                return;
            }
            var keyFolderExplorer = ComboBox_FolderExplorer.SelectedItem.ToString();
            var keyOutline = ComboBox_Outline.SelectedItem.ToString();
//            var keyOutlineMarginLeft = ComboBox_OutlineMarginLeft.SelectedItem.ToString();
            var keyOutlineMarginRight = ComboBox_OutlineMarginRight.SelectedItem.ToString();
            var keyRunLuaExe = ComboBox_RunLuaExe.SelectedItem.ToString();

            System.Collections.ArrayList names = new System.Collections.ArrayList() { };
            names.Add(keyFolderExplorer);
            names.Add(keyOutline);
//            names.Add(keyOutlineMarginLeft);
            names.Add(keyOutlineMarginRight);
            names.Add(keyRunLuaExe);
            System.Collections.ArrayList judge = new System.Collections.ArrayList() { };
            foreach (object ob in names)
            {
                if (judge.BinarySearch(ob) >= 0)
                {
                    System.Windows.MessageBox.Show("Can't set same key binding");
                    return;
                }
                judge.Add(ob);
            }


            BabePackage.Setting.SetBindingKey(SettingConstants.SettingKeys.KeyBindFolder, keyFolderExplorer);
            BabePackage.Setting.SetBindingKey(SettingConstants.SettingKeys.KeyBindOutline, keyOutline);
//            BabePackage.Setting.SetBindingKey("OutlineMarginLeft", keyOutlineMarginLeft);
            BabePackage.Setting.SetBindingKey(SettingConstants.SettingKeys.KeyBindEditorOutlineRight, keyOutlineMarginRight);
            BabePackage.Setting.SetBindingKey(SettingConstants.SettingKeys.KeyBindRunExec, keyRunLuaExe);
            BabePackage.Setting.Save();

            System.Windows.MessageBox.Show("Save key binding");
        }

		private void Link_OpenUserKeywords_Click(object sender, RoutedEventArgs e)
		{
			var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.UserKeywordsFile);
			if (File.Exists(file)) DTEHelper.Current.OpenDocument(file);
		}

		private void Link_OpenSettingFolder_Click(object sender, RoutedEventArgs e)
		{
			var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);
			try
			{
				System.Diagnostics.Process.Start("Explorer.exe", string.Format("/select,{0}", file));
			}
			catch
			{
			}
		}

		private void Link_OpenSettings_Click(object sender, RoutedEventArgs e)
		{
			var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.SettingFile);
			if (File.Exists(file)) DTEHelper.Current.OpenDocument(file);
		}
    }
}
