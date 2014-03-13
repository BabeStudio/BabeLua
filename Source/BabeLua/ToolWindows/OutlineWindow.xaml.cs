using LuaLanguage.DataModel;
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

namespace Babe.Lua.ToolWindows
{
    /// <summary>
    /// OutlineWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OutlineWindow : UserControl
    {
        List<LuaFile> SearchResults = null;
        bool IsSearch = true;

        public OutlineWindow()
        {
            InitializeComponent();

            //TreeView.AddHandler(TreeViewItem.PreviewMouseDownEvent, new MouseButtonEventHandler(TreeView_MouseDown));
            TreeView.AddHandler(TreeViewItem.MouseDoubleClickEvent, new MouseButtonEventHandler(TreeView_MouseDoubleClick));
            //ListBox_SearchResult.AddHandler(Control.PreviewMouseDownEvent, new MouseButtonEventHandler(ListBox_SearchResult_MouseDown));
            
            Refresh();
        }

        public void Refresh()
        {
            //this.FileListBox.ItemsSource = null;
            //this.TextBox_Search.Clear();

            if (FileManager.Instance.Files.Count != 0)
            {
                var list = FileManager.Instance.Files;
                //list.Sort();
                this.FileListBox.ItemsSource = list;

                this.IsEnabled = true;
            }
            else
            {
                this.FileListBox.ItemsSource = null;
                this.TextBox_Search.Text = "Search File";
                this.IsEnabled = false;
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("FileList_SelectionChanged");
            TreeView.Items.Clear();

            if (FileListBox.SelectedItem == null)
            {
                return;
            }
            
            CurrentFile = FileListBox.SelectedItem as LuaFile;

            IsSearch = false;
            TextBox_Search.Text = CurrentFile.ToString();
            IsSearch = true;

            var members = CurrentFile.Members;

            List<TreeViewItem> list = new List<TreeViewItem>();

            foreach (var member in CurrentFile.Members)
            {
                if (member is LuaTable)
                {
                    TreeViewItem tb = new TreeViewItem();
                    tb.Header = member;
                    tb.IsExpanded = true;
                    foreach (var m in (member as LuaTable).Members)
                    {
                        tb.Items.Add(new TreeViewItem() { Header = m });
                    }
                    list.Add(tb);
                }
                else
                {
                    list.Add(new TreeViewItem() { Header = member });
                }
            }

            foreach (var item in list)
            {
                TreeView.Items.Add(item);
            }
        }

        LuaFile CurrentFile;

        private void TreeView_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            System.Diagnostics.Debug.WriteLine("select：" + TreeView.SelectedItem);

            if (TreeView.SelectedItem == null) return;

            var item = TreeView.SelectedItem as TreeViewItem;

            {
                LuaMember m = item.Header as LuaMember;
                if (m != null)
                {
                    DTEHelper.Current.GoTo(CurrentFile.File, m.Line);

                    //DTEHelper.Current.PreviewDocument(CurrentFile.File);
                }
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("double click：" + TreeView.SelectedItem);

            if (e.OriginalSource is Grid)
            {
                return;
            }

            var item = TreeView.SelectedItem as TreeViewItem;

            if (item != null && !item.HasItems)
            {
                item.IsSelected = true;

                var m = item.Header as LuaMember;

                if (m != null)
                {
                    DTEHelper.Current.GoTo(CurrentFile.File, m.Line);
                }
            }
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

        private void EndSearch(object SelectedItem)
        {
            FileListBox.SelectedItem = SelectedItem;

            ListBox_SearchResult.Items.Clear();
            ListBox_SearchResult.Visibility = System.Windows.Visibility.Collapsed;

            TextBox_Search.Text = SelectedItem.ToString();
        }

        private void StartSearch()
        {
            if (!TextBox_Search.IsFocused) return;

            if (string.IsNullOrWhiteSpace(TextBox_Search.Text))
            {
                ListBox_SearchResult.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                ListBox_SearchResult.Visibility = System.Windows.Visibility.Visible;

                string txt = TextBox_Search.Text.Trim().ToLower();

                SearchResults = new List<LuaFile>();
                ListBox_SearchResult.Items.Clear();
                foreach (LuaFile f in FileManager.Instance.Files)
                {
                    var name = f.ToString();

                    if (name.IndexOf(txt, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        SearchResults.Add(f);
                        ListBox_SearchResult.Items.Add(new SearchListItem(name, txt));
                    }
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsSearch) StartSearch();
        }

        private void TextBox_Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (SearchResults.Count() > 0)
                {
                    if (ListBox_SearchResult.SelectedIndex == -1)
                    {
                        ListBox_SearchResult.SelectedIndex = 0;
                    }
                    else if(ListBox_SearchResult.SelectedIndex < SearchResults.Count() - 1)
                    {
                        ListBox_SearchResult.SelectedIndex += 1;
                        ListBox_SearchResult.ScrollIntoView(ListBox_SearchResult.SelectedItem);
                    }
                }
            }
            else if (e.Key == Key.Up)
            {
                if (SearchResults.Count() > 0)
                {
                    if (ListBox_SearchResult.SelectedIndex > 0)
                    {
                        ListBox_SearchResult.SelectedIndex -= 1;
                        ListBox_SearchResult.ScrollIntoView(ListBox_SearchResult.SelectedItem);
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (ListBox_SearchResult.SelectedItem != null)
                {
                    EndSearch(SearchResults[ListBox_SearchResult.SelectedIndex]);
                }
            }
        }

        private void ListBox_SearchResult_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBox_SearchResult.SelectedItem != null)
            {
                EndSearch(SearchResults[ListBox_SearchResult.SelectedIndex]);
            }
        }

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

        private void TreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && TreeView.SelectedItem != null)
            {
                var item = TreeView.SelectedItem as TreeViewItem;

                if (item != null)
                {
                    var m = item.Header as LuaMember;

                    if (m != null)
                    {
                        DTEHelper.Current.GoTo(CurrentFile.File, m.Line);
                    }
                }
            }
        }

        private void TextBox_Search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox_Search.Text))
            {
                TextBox_Search.Text = "Search File";
            }
        }

        private void TextBox_Search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox_Search.Text == "Search File")
            {
                TextBox_Search.Clear();
            }
        }
    }
}
