using LuaLanguage.DataModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Babe.Lua
{
    public partial class SearchToolControl : UserControl
    {
        SolidColorBrush brush1 = new SolidColorBrush(Color.FromArgb(32,0,0,0));
        SolidColorBrush brush2 = new SolidColorBrush(Color.FromArgb(0,255,255,255));

        public SearchToolControl()
        {
            InitializeComponent();
            current = brush1;

            ListView.DragEnter += (s, e) => { };
        }

        Brush current;
        Brush GetNextBrush()
        {
            if (current == brush2)
            {
                current = brush1;
            }
            else
            {
                current = brush2;
            }
            return current;
        }

        internal void Refresh(IEnumerable<LuaMember> list)
        {
            ListView.Items.Clear();
            int i = 0;
            var brush = GetNextBrush();

            string curFilePath = "";
            foreach (var item in list)
            {
                if (curFilePath != item.File.File)
                {
                    brush = GetNextBrush();
                    curFilePath = item.File.File;
                }

                var ltim = new SearchListItem(item, (++i).ToString().PadRight(4));
                ltim.Background = brush;
                ListView.Items.Add(ltim);
            }
        }

		private void Search()
		{
			var txt = TextBox_SearchWord.Text;
			if (string.IsNullOrWhiteSpace(txt)) return;

			if (!txt.Any(ch => { return ch.IsWord(); })) return;

			if (BabePackage.Setting.ContainsSearchFilter(txt)) return;

			DTEHelper.Current.RefreshSearchWnd(txt, true);
		}

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListView.SelectedItem != null)
            {
                var item = (SearchListItem)(ListView.SelectedItem);
                DTEHelper.Current.OpenDocument(item.token.File.File);
                DTEHelper.Current.GoTo(item.token.File.File, item.token.Line, item.token.Column, item.token.Name.Length, true);
/*                var state = Keyboard.GetKeyStates(Key.LeftCtrl);
                if (state.HasFlag(KeyStates.Down))
                {
                }*/
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListView.SelectedItem != null)
            {
                //定位到选择的位置
                var item = (SearchListItem)(ListView.SelectedItem);
                
                DTEHelper.Current.GoTo(item.token.File.File, item.token.Line, item.token.Column, item.token.Name.Length, true);
            }
        }

		private void Button_ClearResult_Click(object sender, RoutedEventArgs e)
		{
			ListView.Items.Clear();
			DTEHelper.Current.RefreshSearchWnd("", false);
		}

		private void Button_Search_Click(object sender, RoutedEventArgs e)
		{
			Search();
		}

		private void Button_CopyAllResult_Click(object sender, RoutedEventArgs e)
		{
			var results = new StringBuilder();
			foreach (var item in ListView.Items)
			{
				results.AppendLine(item.ToString());
			}

			Clipboard.SetText(results.ToString());
		}

		private void TextBox_SearchWord_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Search();
			}
		}
    }

    
}