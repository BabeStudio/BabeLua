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
	/// FolderWindowItem.xaml 的交互逻辑
	/// </summary>
	public partial class FolderWindowItem : TreeViewItem
	{
		public string FileName { get; private set; }

		bool m_isFolder;
		public bool IsFolder
		{
			get
			{
				return m_isFolder;
			}
			set
			{
				m_isFolder = value;

				if (IsFolder)
				{
					SetValue(IconProperty, this.Resources["Icon_Folder"]);
				}
				else
				{
					SetValue(IconProperty, this.Resources["Icon_Lua"]);
				}
			}
		}

		public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(FolderWindowItem));

		public FolderWindowItem()
		{
			IsFolder = false;
		}
		
		public FolderWindowItem(string Name, bool IsFolder = false)
		{
			InitializeComponent();this.Style = Resources["TreeViewItemStyle"] as Style;
			SetFileName(Name);
			this.IsFolder = IsFolder;
		}

		public void SetFileName(string name)
		{
			this.FileName = name;
			this.Header = name;
		}
	}
}
