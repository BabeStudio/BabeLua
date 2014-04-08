using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows.Input;
using Babe.Lua.Package;
using System;
using System.Windows.Threading;

namespace Babe.Lua.ToolWindows
{
    /// <summary>
    /// FolderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FolderWindow : UserControl
    {
		FileSystemWatcher FileWatcher;
        FileSystemWatcher FolderWatcher;

        public FolderWindow()
        {
            InitializeComponent();

            this.IsEnabled = false;

            TreeView.AddHandler(TreeViewItem.MouseRightButtonDownEvent, new MouseButtonEventHandler(TreeView_MouseDown));
            TreeView.AddHandler(TreeViewItem.MouseDoubleClickEvent, new MouseButtonEventHandler(TreeView_MouseDoubleClick));

			FileWatcher = new FileSystemWatcher();
			FileWatcher.Filter = "*.lua";
			FileWatcher.EnableRaisingEvents = false;
            FileWatcher.IncludeSubdirectories = true;
			FileWatcher.NotifyFilter = NotifyFilters.FileName;
			FileWatcher.Created += File_Created;
			FileWatcher.Deleted += File_Deleted;
			FileWatcher.Renamed += File_Renamed;

            FolderWatcher = new FileSystemWatcher();
            FolderWatcher.Filter = "";
            FolderWatcher.EnableRaisingEvents = false;
            FolderWatcher.IncludeSubdirectories = true;
            FolderWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            FolderWatcher.Created += Folder_Created;
            FolderWatcher.Deleted += Folder_Deleted;
            FolderWatcher.Renamed += Folder_Renamed;
            Refresh();
        }

        #region FileSystemWatcher
        private void Folder_Renamed(object sender, RenamedEventArgs e)
        {
            var oldName = e.OldFullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");
            var newName = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, ""); 

            HashSet<string> List = new HashSet<string>();

            Files.RemoveWhere((f) => 
            {
                if (f.StartsWith(oldName))
                {
                    List.Add(f.Replace(oldName, newName));
                    return true;
                }
                else
                {
                    return false;
                }
            });

            foreach (string f in List)
            {
                Files.Add(f);
            }

            AsyncFileChangesToTreeView(oldName, Path.GetFileName(newName), true);
        }

        private void Folder_Deleted(object sender, FileSystemEventArgs e)
        {
            var name = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");

            Files.RemoveWhere((str) => { return str.StartsWith(name); });

            AsyncFileChangesToTreeView(e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, ""), null, true);
        }

        private void Folder_Created(object sender, FileSystemEventArgs e)
        {
            var attributes = File.GetAttributes(e.FullPath);
            if (attributes.HasFlag(FileAttributes.Hidden)) return;

            var filename = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");

            AsyncFileChangesToTreeView(null, filename, true);
        }

		private void File_Renamed(object sender, RenamedEventArgs e)
		{
			var oldName = e.OldFullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");
			var newName = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");

			if (Files.Contains(oldName) && !Files.Contains(newName))
			{
				AsyncFileChangesToTreeView(oldName, Path.GetFileName(newName), false);
				Files.Remove(oldName);
				Files.Add(newName);
			}
		}

		private void File_Deleted(object sender, FileSystemEventArgs e)
		{
            var name = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");

            if (Files.Contains(name))
            {
                AsyncFileChangesToTreeView(name, null, false);
                Files.Remove(name);
            }
            
		}

		private void File_Created(object sender, FileSystemEventArgs e)
		{
            var attributes = File.GetAttributes(e.FullPath);
            if (attributes.HasFlag(FileAttributes.Hidden)) return;

            var filename = e.FullPath.Replace(LuaPath + Path.DirectorySeparatorChar, "");

			if (!Files.Contains(filename))
			{
                AsyncFileChangesToTreeView(null, filename, false);
				Files.Add(filename);
			}
		}

		void AsyncFileChangesToTreeView(string oldName, string newName, bool isFolder)
		{
            this.Dispatcher.Invoke(() =>
            {
                if (oldName != null)
                {
                    var item = FindItemInTree(oldName);
                    if (item != null)
                    {
                        if (newName == null)
                        {
                            (item.Parent as ItemsControl).Items.Remove(item);
                        }
                        else
                        {
                            (item as FolderWindowItem).SetFileName(newName);
                        }
                    }
                }
                else
                {
                    var layers = newName.Split(Path.DirectorySeparatorChar);
                    var pat = TreeView as ItemsControl;

                    //找到新建项的位置，添加项
                    for (int i = 0; i < layers.Length - 1; i++)
                    {
                        bool find = false;
                        foreach (FolderWindowItem item in pat.Items)
                        {
                            if (item.FileName == layers[i])
                            {
                                pat = item;
                                find = true;
                                break;
                            }
                        }
                        if (find) continue;
                        var folder = new FolderWindowItem(layers[i], true);
                        pat.Items.Add(folder);
                        pat = folder;
                    }
                    foreach (FolderWindowItem item in pat.Items)
                    {
                        if (item.FileName == layers.Last()) return;
                    }
                    var file = new FolderWindowItem(layers.Last(), isFolder);
                    pat.Items.Add(file);
                }
            });
		}
        #endregion

        string LuaPath;
        HashSet<string> Files;
        List<string> SearchResults;
        
        public void Refresh()
        {
            var set = BabePackage.Current.CurrentSetting;
            
            if (set == null || string.IsNullOrWhiteSpace(set.Folder))
            {
                TreeView.Items.Clear();

                Files = null;
                LuaPath = null;

                this.IsEnabled = false;
				FileWatcher.EnableRaisingEvents = false;
                FolderWatcher.EnableRaisingEvents = false;
            }
            else if(set.Folder != LuaPath)
            {
                LuaPath = set.Folder;
                this.IsEnabled = true;

                System.Diagnostics.Debug.Print("change");

				TreeView.Items.Clear();
                Files = new HashSet<string>();

				var list = MakeTree(set.Folder);

				foreach (var l in list)
				{
					TreeView.Items.Add(l);
				}

                FileWatcher.Path = set.Folder;
				FileWatcher.EnableRaisingEvents = true;
                FolderWatcher.Path = set.Folder;
                FolderWatcher.EnableRaisingEvents = true;
            }
        }

        List<FolderWindowItem> MakeTree(string folder)
        {
            var list = new List<FolderWindowItem>();

            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                if (File.GetAttributes(f).HasFlag(FileAttributes.Hidden)) continue;
				var item = new FolderWindowItem(f.Substring(f.LastIndexOf(Path.DirectorySeparatorChar) + 1), true);
                
                foreach (var l in MakeTree(Path.Combine(folder, f)))
                {
                    item.Items.Add(l);
                }
                list.Add(item);
            }

			foreach (var f in Directory.EnumerateFiles(folder, "*.lua").Where((name) => { return name.ToLower().EndsWith(".lua"); }))
			{
				if (File.GetAttributes(f).HasFlag(FileAttributes.Hidden)) continue;

				list.Add(new FolderWindowItem(Path.GetFileName(f)));

				Files.Add(f.Replace(LuaPath + Path.DirectorySeparatorChar, ""));
			}

            return list;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!TextBox.IsFocused) return;

            string word = TextBox.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(word))
            {
                //TreeView.Visibility = System.Windows.Visibility.Visible;
                SearchView.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            if (Files == null || Files.Count == 0) return;

            SearchResults = new List<string>();
            SearchView.Items.Clear();
            foreach (string f in Files)
            {
                if (Path.GetFileName(f).IndexOf(word, System.StringComparison.OrdinalIgnoreCase) != -1)
                {
                    SearchResults.Add(f);
                    SearchView.Items.Add(new SearchListItem(f, word));
                }
            }
            
            if (SearchView.Visibility != System.Windows.Visibility.Visible)
            {
                SearchView.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox.Text))
            {
                TextBox.Text = "Search File";
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox.Text.Equals("Search File"))
            {
                TextBox.Clear();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender == Menu_NewFile)
            {
                ItemsControl parent = TreeView;
				//获取创建位置
                if (TreeView.SelectedItem != null) 
                {
                    var pat = TreeView.SelectedItem as FolderWindowItem;
                    if (pat.IsFolder)
                    {
                        parent = pat;
                    }
					else
					{
						parent = pat.Parent as ItemsControl;
					}
                }
				
                var item = new FolderWindowItem(string.Format("File{0}.lua",FindStartIndex(parent, "File", false)));
                parent.Items.Add(item);
                if (parent is TreeViewItem)
                {
                    (parent as TreeViewItem).IsExpanded = true;
                }
                parent.UpdateLayout();
                EditItemHeader(item, true);
            }
            else if (sender == Menu_NewFolder)
            {
                ItemsControl parent = TreeView;
                if (TreeView.SelectedItem != null)
                {
                    var pat = TreeView.SelectedItem as FolderWindowItem;

                    if (pat.IsFolder)
                    {
                        parent = pat;
                    }
					else
					{
						parent = pat.Parent as ItemsControl;
					}
                }

				var item = new FolderWindowItem(string.Format("Folder{0}", FindStartIndex(parent, "Folder", true)), true);
                parent.Items.Add(item);
                if (parent is TreeViewItem)
                {
                    (parent as TreeViewItem).IsExpanded = true;
                }
                parent.UpdateLayout();
                EditItemHeader(item, true);
                
            }
            else if (sender == Menu_ReName && TreeView.SelectedItem != null)
            {
                EditItemHeader(TreeView.SelectedItem as FolderWindowItem, false);
            }
            else if (sender == Menu_Delete && TreeView.SelectedItem != null)
            {
                var item = TreeView.SelectedItem as FolderWindowItem;
                if (Delete(GetItemPath(item), item.FileName, item.IsFolder))
                {
                    (item.Parent as ItemsControl).Items.Remove(item);
                }
            }
			else if (sender == Menu_OpenContainFolder)
			{
				string path = LuaPath;

				if (TreeView.SelectedItem != null)
				{
					var item = TreeView.SelectedItem as FolderWindowItem;

					path = GetItemPath(item);
					
					path = Path.Combine(path, item.FileName);
				}

				try
				{
					System.Diagnostics.Process.Start("Explorer.exe", string.Format("/select,{0}", path));
				}
				catch
				{
					MessageBox.Show("open fail","Error");
				}
			}
        }

		private DispatcherTimer _timerPreviewDocument;
        private string _strPreviewDocumentPath;
        private void SetPreviewDocumentTimerStart(string path)
        {
            _strPreviewDocumentPath = path;
			_timerPreviewDocument = new DispatcherTimer();
            // 循环间隔时间
            _timerPreviewDocument.Interval = TimeSpan.FromMilliseconds(1);
            // 允许Timer执行
			_timerPreviewDocument.Start();
            // 定义回调
			_timerPreviewDocument.Tick += PreviewDocument;
        }
        // timer事件
        private void PreviewDocument(object sender, EventArgs e)
        {
            if (File.Exists(_strPreviewDocumentPath))
            {
                DTEHelper.Current.PreviewDocument(_strPreviewDocumentPath);
            }
			_timerPreviewDocument.Stop();
        }

        private void TreeView_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeView.SelectedItem != null)
            {
                var item = (FolderWindowItem)TreeView.SelectedItem;
                if (!item.IsFolder)
                {
                    var path = Path.Combine(GetItemPath(item), item.FileName);

                    //启动打开文件预览定时器
                    SetPreviewDocumentTimerStart(path);
                }
                //else
                //{
                //    item.IsExpanded = !item.IsExpanded;
                //}
            }
        }

        private TreeViewItem FindItemInChildren(TreeViewItem Father, string name)
        {
            foreach (FolderWindowItem item in Father.Items)
            {
                if (item.FileName.Equals(name)) return item;               
                else if (name.StartsWith(item.FileName) && item.Items.Count > 0)
                {
                    var res = FindItemInChildren(item, name.Substring(item.FileName.Length + 1));
                    if (res != null) return res;
                }
            }
            return null;
        }

        private TreeViewItem FindItemInTree(string name)
        {
            foreach (FolderWindowItem item in TreeView.Items)
            {
                if (item.FileName.Equals(name)) return item;
                else if (name.StartsWith(item.FileName) && item.Items.Count > 0)
                {
                    var res = FindItemInChildren(item, name.Substring(item.FileName.Length + 1));
                    if (res != null) return res;
                }
            }
            return null;
        }

		int FindStartIndex(ItemsControl parent, string head, bool isFolder)
		{
			int cur = 0;
			int max = 0;

			foreach (FolderWindowItem item in parent.Items)
			{
				if (item.IsFolder == isFolder && item.FileName.StartsWith(head) && item.FileName.Length > head.Length)
				{
					string num = item.FileName.Substring(head.Length);
					if (!isFolder)
					{
						num = Path.GetFileNameWithoutExtension(num);
					}
					if (int.TryParse(num, out cur) && cur > max)
					{
						max = cur;
					}
				}
			}

			return max + 1;
		}

        private void EndSearch(object SelectedItem)
        {
            string file = null;
            if (SelectedItem != null)
            {
                file = SearchResults[SearchView.SelectedIndex];
            }
            else
	        {
                file = SearchResults[SearchView.SelectedIndex];
	        }

            var item = FindItemInTree(file);
            if (item != null)
            {
                TreeViewItem par = item.Parent as TreeViewItem;
                while (par != null)
                {
                    par.IsExpanded = true;
                    par = par.Parent as TreeViewItem;
                }
                item.IsSelected = true;
                item.BringIntoView();

                DTEHelper.Current.OpenDocument(Path.Combine(LuaPath, file));
            }

            TextBox.Text = "Search File";

            SearchView.Items.Clear();
            SearchView.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = GetTemplatedAncestor<TreeViewItem>(e.OriginalSource as FrameworkElement);
            if (item != null)
            {
                //if (e.LeftButton == MouseButtonState.Pressed)
                //{
                //    var txtbox = item.Template.FindName("PART_Header", item) as TextBox;
                //    if (txtbox.IsEnabled) return;

                //    if (item.Tag != null && item.Tag.ToString() == "file")
                //    {
                //        var path = Path.Combine(GetItemPath(item), item.Header.ToString());

                //        if (File.Exists(path))
                //        {
                //            if (e.ClickCount > 1)
                //            {
                //                DTEHelper.Current.OpenDocument(path);
                //            }
                //            else
                //            {
                //                DTEHelper.Current.PreviewDocument(path);
                //            }
                //        }
                //    }
                //    else
                //    {
                //        item.IsExpanded = !item.IsExpanded;
                //    }
                //}
                //else
                //{
                //    item.IsSelected = true;
                //}
                item.Focus();
            }
            else if (TreeView.SelectedItem != null)
            {
                (TreeView.SelectedItem as TreeViewItem).IsSelected = false;
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = GetTemplatedAncestor<FolderWindowItem>(e.OriginalSource as FrameworkElement);
            if (item != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var txtbox = item.Template.FindName("PART_Header", item) as TextBox;
                    if (txtbox.IsEnabled) return;

                    if (!item.IsFolder)
                    {
                        var path = Path.Combine(GetItemPath(item), item.FileName);

                        if (File.Exists(path))
                        {
                            DTEHelper.Current.OpenDocument(path);
                        }
                    }
                }
                else
                {
                    item.IsSelected = true;
                }
            }
            else if (TreeView.SelectedItem != null)
            {
                (TreeView.SelectedItem as TreeViewItem).IsSelected = false;
            }
            e.Handled = true;
        }

        T GetTemplatedAncestor<T>(FrameworkElement element) where T : FrameworkElement
        {
            if (element is T)
            {
                return element as T;
            }

            FrameworkElement templatedParent = element.TemplatedParent as FrameworkElement;
            if (templatedParent != null)
            {
                return GetTemplatedAncestor<T>(templatedParent);
            }

            return null;
        }

		FolderWindowItem edit_item;
		bool edit_isNewFile;
		TextBox edit_txtbox;
        void EditItemHeader(FolderWindowItem item, bool IsNewFile)
        {
            var txtbox = item.Template.FindName("PART_Header", item) as TextBox;
            if (txtbox == null) return;

            txtbox.IsEnabled = true;
            
            Keyboard.Focus(txtbox);

            var dot = txtbox.Text.LastIndexOf('.');
            if (dot == -1) txtbox.SelectAll();
            else txtbox.Select(0, dot);

            txtbox.KeyDown += txtbox_KeyDown;

			edit_isNewFile = IsNewFile;
			edit_item = item;
			edit_txtbox = txtbox;
			txtbox.LostFocus += txtbox_LostFocus;
        }

		void txtbox_LostFocus(object sender, RoutedEventArgs e)
		{
			edit_txtbox.LostFocus -= txtbox_LostFocus;
			edit_txtbox.KeyDown -= txtbox_KeyDown;

			edit_txtbox.IsEnabled = false;
			var oldName = edit_item.FileName;

			if (edit_isNewFile)
			{
				var path = Path.Combine(GetItemPath(edit_item), edit_txtbox.Text);

				try
				{
					if (edit_item.IsFolder)
					{
						NewFolder(path);
					}
					else
					{
						NewFile(path);
					}

					edit_item.SetFileName(edit_txtbox.Text);
				}
				catch (System.Exception ex)
				{
					BabePackage.Setting.LogError(ex);
					MessageBox.Show(string.Format("create {0} fail.", edit_txtbox.Text), "Error");
					(edit_item.Parent as ItemsControl).Items.Remove(edit_item);
				}
			}
			else if (edit_txtbox.Text != oldName)
			{
				try
				{
					ReName(GetItemPath(edit_item), oldName, edit_txtbox.Text, edit_item.IsFolder);

					edit_item.SetFileName(edit_txtbox.Text);
				}
				catch (System.Exception ex)
				{
					edit_txtbox.Text = oldName;
					BabePackage.Setting.LogError(ex);
					MessageBox.Show("rename fail.", "Error");
				}
			}
		}

        void txtbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TreeView.Focus();
				((sender as FrameworkElement).TemplatedParent as TreeViewItem).IsSelected = true;
            }
        }

        string GetItemPath(FolderWindowItem item)
        {
            List<string> paths = new List<string>();

            var pat = (item.Parent as FolderWindowItem);

            while (pat != null)
            {
                paths.Add(pat.FileName);
                pat = (pat.Parent as FolderWindowItem);
            }
            paths.Add(LuaPath);
            paths.Reverse();

            string path = Path.Combine(paths.ToArray());

            return path;
        }

        #region File Opeartion
        void ReName(string path, string oldName, string newName, bool isFolder)
        {
            oldName = Path.Combine(path, oldName);
            newName = Path.Combine(path, newName);

            if (!isFolder)
            {
                File.Move(oldName, newName);

                RefreshFiles(oldName.Replace(LuaPath + Path.DirectorySeparatorChar, ""), newName.Replace(LuaPath + Path.DirectorySeparatorChar, ""));
			}
            else
            {
				Directory.Move(oldName, newName);
            }
        }

        bool Delete(string path, string name, bool isFolder)
        {
            if (MessageBox.Show(string.Format(Properties.Resources.DeleteFileConfirm, name), "BabeLua", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                name = Path.Combine(path, name);
                if (!isFolder)
                {
                    File.Delete(name);
					RefreshFiles(name.Replace(LuaPath + Path.DirectorySeparatorChar, ""), null);
                    return true;
                }
                else
                {
                    Directory.Delete(name, true);
					//RefreshFiles(name.Replace(LuaPath + Path.DirectorySeparatorChar, ""), null);
                    return true;
                }
            }
            return false;
        }

        void NewFile(string path)
        {
            try
            {
				var stream = new FileStream(path, FileMode.CreateNew);
				
				//检测设置项，确定新建文件的编码格式
				//由于不含中文的ANSI和UTF8编码一致，导致此文件会被VS认为是ANSI文件。
				Encoding encoding;
				switch (BabePackage.Current.CurrentSetting.Encoding)
				{
					case Editor.EncodingName.UTF8:
						encoding = new UTF8Encoding(false);
						break;
					case Editor.EncodingName.UTF8_BOM:
						encoding = Encoding.UTF8;
						break;
					case Editor.EncodingName.ANSI:
						encoding = Encoding.Default;
						break;
					default:
						encoding = new UTF8Encoding(false);
						break;
				}

				using (var writer = new StreamWriter(stream, encoding))
				{
					try
					{
						writer.WriteLine(string.Format("--region {0}", Path.GetFileName(path)));
						writer.WriteLine(string.Format("--Date {0}", System.DateTime.Now.Date.ToShortDateString()));
						//writer.WriteLine("--此文件由[BabeLua]插件自动生成");
						writer.WriteLine();
						writer.WriteLine();
						writer.WriteLine();
						writer.WriteLine("--endregion");
					}
					catch { }
				}
				
				RefreshFiles(null, path.Replace(LuaPath + Path.DirectorySeparatorChar, ""));
            }
            catch 
            {
				throw;
            }
        }

        void NewFolder(string path)
        {
			if (Directory.Exists(path)) throw new System.Exception("user operation exception.");
            try
            {
                Directory.CreateDirectory(path);
				//RefreshFiles(null, path.Replace(LuaPath + Path.DirectorySeparatorChar, ""));
            }
            catch 
            {
				throw;
            }
        }

        void RefreshFiles(string oldName, string newName)
        {
            if (Files == null) return;

            if (oldName != null && Files.Contains(oldName))
            {
                Files.Remove(oldName);
				System.Diagnostics.Debug.Print("old:" + oldName);
            }
            if (newName != null && !Files.Contains(newName))
            {
                Files.Add(newName);
				System.Diagnostics.Debug.Print("new:" + newName);
            }
        }
        #endregion

        private double treeViewHorizScrollPos = 0.0;
        private bool treeViewResetHorizScroll = false;
        private ScrollViewer treeViewScrollViewer = null;
        private void TreeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            if (treeViewScrollViewer == null)
            {
                treeViewScrollViewer = TreeView.Template.FindName("_tv_scrollviewer_", TreeView) as ScrollViewer;
                if (this.treeViewScrollViewer != null)
                    this.treeViewScrollViewer.ScrollChanged += new ScrollChangedEventHandler(TreeViewScrollViewerScrollChanged);
            }
            this.treeViewResetHorizScroll = true;
            this.treeViewHorizScrollPos = this.treeViewScrollViewer.HorizontalOffset;
        }
        private void TreeViewScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (this.treeViewResetHorizScroll)
                this.treeViewScrollViewer.ScrollToHorizontalOffset(this.treeViewHorizScrollPos);

            this.treeViewResetHorizScroll = false;
        }

        private void TextBox_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (SearchResults.Count > 0)
                {
                    if (SearchView.SelectedIndex == -1)
                    {
                        SearchView.SelectedIndex = 0;
                    }
                    else if (SearchView.SelectedIndex < SearchResults.Count - 1)
                    {
                        SearchView.SelectedIndex += 1;
                        SearchView.ScrollIntoView(SearchView.SelectedItem);
                    }
                }
            }
            else if (e.Key == Key.Up)
            {
                if (SearchResults.Count > 0)
                {
                    if (SearchView.SelectedIndex > 0)
                    {
                        SearchView.SelectedIndex -= 1;
                        SearchView.ScrollIntoView(SearchView.SelectedItem);
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (SearchView.SelectedItem != null)
                {
                    EndSearch(null);
                }
            }
        }

        private void ListView_SearchResult_MouseDown(object sender, MouseButtonEventArgs e)
        {
            EndSearch(sender);
        }
    }
}
