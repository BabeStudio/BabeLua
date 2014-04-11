using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Babe.Lua.Editor;

namespace Babe.Lua.Package
{
	public static class SettingConstants
	{
		public const string Version = "W.1.0.7";

		public const string SettingFolder = "BabeLua";
		public const string SettingFile = "Setting.xml";
		public const string UserKeywordsFile = "UserKeyWords.xml";
		public const string ErrorLogFile = "ErrorLog.txt";
		public const string GuidFile = "Guid";

		public static class SettingKeys
		{
			public const string KeyBinding = "KeyBinding";
			public const string KeyBindFolder = "FolderExplorer";
			public const string KeyBindOutline = "Outline";
			public const string KeyBindEditorOutlineLeft = "OutlineMarginLeft";
			public const string KeyBindEditorOutlineRight = "OutlineMarginRight";
			public const string KeyBindRunExec = "RunLuaExe";

			public const string SearchFilter = "SearchFilters";

			public const string UISetting = "UISettings";
			public const string HideVSView = "HideUselessView";

			public const string Highlight = "Highlight";
			public const string Table = "Table";
			public const string Function = "Function";

			public const string ActiveOpendFile = "Active";
			public const string ActiveOpendFileLine = "Line";
			public const string ActiveOpendFileColumn = "Column";
			public const string OpendFile = "File";

			public const string LuaSetting = "LuaSettings";
			public const string CurrentSet = "CurrentSet";
			public const string Set = "Set";
			public const string SetName = "Name";
			public const string LuaFolder = "Folder";
			public const string LuaExec = "LuaExecutable";
			public const string WorkingPath = "WorkingPath";
			public const string LuaExecArg = "CommandLine";
			public const string FileEncoding = "FileEncoding";

			public const string Keywords = "Keywords";
			public const string ClassDefinition = "ClassDefinition";
			public const string ClassConstructor = "ClassConstructor";
		}

		public static class KeywordsKeys
		{
			public const string C = "C";
			public const string Lua = "LuaFramework";
			public const string R = "r";
			public const string G = "g";
			public const string B = "b";
			public const string User = "User";
		}
	}

	class Setting
	{
		static Setting _instance;
		public static Setting Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Setting();
				}

				return _instance;
			}
		}

		public KeyWordSettings KeyWords;

		XElement XMLOpenFiles;
		XElement XMLLuaSettings;
		XElement XMLKeyBinding;
		XElement XMLSearchFilters;
		XElement XMLUISettings;

		public Dictionary<string, LuaSet> LuaSettings { get; private set; }

		public bool IsFirstRun { get; set; }
		public string UserGUID { get; private set; }

		public bool HideUselessViews
		{
			get
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.HideVSView);
				if (Element == null) return false;
				return Element.Value == "1" ? true : false;
			}
			set
			{
				var Element = XMLUISettings.Element(SettingConstants.SettingKeys.HideVSView);
				if (Element == null) XMLUISettings.Add(new XElement(SettingConstants.SettingKeys.HideVSView, Convert.ToInt32(value)));
				else Element.Value = Convert.ToInt32(value).ToString();
			}
		}

		public string ClassDefinition { get; private set; }
		public string ClassConstructor { get; private set; }

		XDocument Doc;
		string FileName;

		private Setting()
		{
			string dic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder);
			FileName = Path.Combine(dic, SettingConstants.SettingFile);

			if (!Directory.Exists(dic))
			{
				IsFirstRun = true;
				Directory.CreateDirectory(dic);
			}
			CreateSettings(dic);

			KeyWords = KeyWordSettings.Instance;

			if (File.Exists(FileName))
			{
				Doc = XDocument.Load(FileName);

				//OpenFiles = Doc.Root.Element("OpenFile");
				//if (OpenFiles == null) OpenFiles = new XElement("OpenFile");

				XMLLuaSettings = Doc.Root.Element(SettingConstants.SettingKeys.LuaSetting);
				if (XMLLuaSettings == null)
				{
					XMLLuaSettings = new XElement(SettingConstants.SettingKeys.LuaSetting);
					Doc.Root.Add(XMLLuaSettings);
				}
				InitLuaSetting();

				XMLKeyBinding = Doc.Root.Element(SettingConstants.SettingKeys.KeyBinding);
				if (XMLKeyBinding == null)
				{
					XMLKeyBinding = new XElement(SettingConstants.SettingKeys.KeyBinding);
					Doc.Root.Add(XMLKeyBinding);
				}

				XMLSearchFilters = Doc.Root.Element(SettingConstants.SettingKeys.SearchFilter);
				if (XMLSearchFilters == null)
				{
					XMLSearchFilters = new XElement(SettingConstants.SettingKeys.SearchFilter);
					Doc.Root.Add(XMLSearchFilters);
				}

				XMLUISettings = Doc.Root.Element(SettingConstants.SettingKeys.UISetting);
				if (XMLUISettings == null)
				{
					XMLUISettings = new XElement(SettingConstants.SettingKeys.UISetting);
					Doc.Root.Add(XMLUISettings);
				}

				#region Init Keywords
				ClassDefinition = "class";
				ClassConstructor = "new";
				var XMLKeywords = Doc.Root.Element(SettingConstants.SettingKeys.Keywords);
				if (XMLKeywords == null)
				{
					XMLKeywords = new XElement(SettingConstants.SettingKeys.Keywords);
					XMLKeywords.Add(new XElement(SettingConstants.SettingKeys.ClassDefinition, ClassDefinition));
					XMLKeywords.Add(new XElement(SettingConstants.SettingKeys.ClassConstructor, ClassConstructor));
					Doc.Root.Add(XMLKeywords);
				}
				else
				{
					var def = XMLKeywords.Element(SettingConstants.SettingKeys.ClassDefinition);
					if (def != null) ClassDefinition = def.Value;
					var ctor = XMLKeywords.Element(SettingConstants.SettingKeys.ClassConstructor);
					if (ctor != null) ClassConstructor = ctor.Value;
				}
				#endregion

				#region InitHighlight
				XElement XMLHighlight = Doc.Root.Element(SettingConstants.SettingKeys.Highlight);
				if(XMLHighlight == null)
				{
					XMLHighlight = new XElement(SettingConstants.SettingKeys.Highlight);
					XMLHighlight.Add
					(
						new XElement
						(
							SettingConstants.SettingKeys.Function,
							new XAttribute(SettingConstants.KeywordsKeys.R, 0),
							new XAttribute(SettingConstants.KeywordsKeys.G, 0),
							new XAttribute(SettingConstants.KeywordsKeys.B, 0)
						)
					);
					XMLHighlight.Add
					(
						new XElement
						(
							SettingConstants.SettingKeys.Table,
							new XAttribute(SettingConstants.KeywordsKeys.R, 0),
							new XAttribute(SettingConstants.KeywordsKeys.G, 0),
							new XAttribute(SettingConstants.KeywordsKeys.B, 0)
						)
					);
					Doc.Root.Add(XMLHighlight);
				}
				else
				{
					var table = XMLHighlight.Element(SettingConstants.SettingKeys.Table);
					if (table != null)
					{
						var color = Color.FromRgb(
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(table.Attribute(SettingConstants.KeywordsKeys.B).Value));
						if (color.R != 0 || color.B != 0 || color.G != 0)
						{
							KeyWords.Table = color;
						}
					}
					else
					{
						XMLHighlight.Add
						(
							new XElement
							(
								SettingConstants.SettingKeys.Table,
								new XAttribute(SettingConstants.KeywordsKeys.R, 0),
								new XAttribute(SettingConstants.KeywordsKeys.G, 0),
								new XAttribute(SettingConstants.KeywordsKeys.B, 0)
							)
						);
					}
					var function = XMLHighlight.Element(SettingConstants.SettingKeys.Function);
					if (function != null)
					{
						var color = Color.FromRgb(
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(function.Attribute(SettingConstants.KeywordsKeys.B).Value));
						if (color.R != 0 || color.B != 0 || color.G != 0)
						{
							KeyWords.Function = color;
						}
					}
					else
					{
						XMLHighlight.Add
						(
							new XElement
							(
								SettingConstants.SettingKeys.Function,
								new XAttribute(SettingConstants.KeywordsKeys.R, 0),
								new XAttribute(SettingConstants.KeywordsKeys.G, 0),
								new XAttribute(SettingConstants.KeywordsKeys.B, 0)
							)
						);
					}
				}
				#endregion

				Save();
			}

			InitSearchFilterList();
		}

		void CreateSettings(string folder)
		{
			var file = Path.Combine(folder, SettingConstants.SettingFile);
			if (!File.Exists(file))
			{
				using (var stream = File.CreateText(file))
				{
					stream.Write(Properties.Resources.Setting);
				}
			}

			file = Path.Combine(folder, SettingConstants.UserKeywordsFile);
			if (!File.Exists(file))
			{
				using (var stream = File.CreateText(Path.Combine(folder, SettingConstants.UserKeywordsFile)))
				{
					stream.Write(Properties.Resources.UserKeyWords);
				}
			}

			file = Path.Combine(folder, SettingConstants.GuidFile);
			if (!File.Exists(file))
			{
				UserGUID = Guid.NewGuid().ToString();

				using (var stream = File.CreateText(file))
				{
					stream.Write(UserGUID);
				}
			}
			else
			{
				using (var stream = new StreamReader(file))
				{
					UserGUID = stream.ReadToEnd();
				}
			}
		}

		public void Save()
		{
			Doc.Save(FileName);
		}

		public void LogError(Exception e)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);
				
				Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
				json["Version"] = SettingConstants.Version;
				json["Guid"] = UserGUID;
				json["Type"] = e.GetType().FullName;
				json["Time"] = DateTime.Now.ToString();
				json["Position"] = string.Format("{0}--->{1}", e.Source, e.TargetSite);
				json["Message"] = e.Message;
				json["StackTrace"] = e.StackTrace;
				
				using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
				{
					writer.WriteLine(json.ToString());
					writer.WriteLine();
				}
			}
			catch { }
		}

		public void LogError(string message)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder, SettingConstants.ErrorLogFile);

				Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
				json["Version"] = SettingConstants.Version;
				json["Guid"] = UserGUID;
				json["Type"] = "LogMessage";
				json["Time"] = DateTime.Now.ToString();
				json["Message"] = message;
				
				using (StreamWriter writer = new StreamWriter(path, true, new UTF8Encoding(false)))
				{
					writer.WriteLine(json.ToString());
					writer.WriteLine();
				}
			}
			catch { }
		}

		#region OpenFiles
		public List<string> GetOpenFiles(ref string ActiveFile, ref int Line, ref int Column)
		{
			List<string> Files = new List<string>();

			var ActElement = XMLOpenFiles.Element(SettingConstants.SettingKeys.ActiveOpendFile);
			if (ActElement != null)
			{
				ActiveFile = ActElement.Value;
				int.TryParse(ActElement.Attribute(SettingConstants.SettingKeys.ActiveOpendFileLine).Value, out Line);
				int.TryParse(ActElement.Attribute(SettingConstants.SettingKeys.ActiveOpendFileColumn).Value, out Column);
			}

			var Elements = XMLOpenFiles.Elements(SettingConstants.SettingKeys.OpendFile);

			foreach (XElement xl in Elements)
			{
				Files.Add(xl.Value);
			}

			return Files;
		}
		#endregion

		#region Filters
		void InitSearchFilterList()
		{
			SearchFilterList = new HashSet<string>();
			foreach (XElement xe in XMLSearchFilters.Elements())
			{
				SearchFilterList.Add(xe.Name.LocalName);
			}
		}

		HashSet<string> SearchFilterList;
		public bool ContainsSearchFilter(string name)
		{
			return SearchFilterList.Contains(name);
		}

		public void SetSearchFilters(string xml)
		{
			try
			{
				XMLSearchFilters = XElement.Parse(xml);
				Doc.Root.ReplaceWith(XMLSearchFilters);
				InitSearchFilterList();
			}
			catch { }
		}
		#endregion

		#region LuaSetting
		public void InitLuaSetting()
		{
			LuaSettings = new Dictionary<string, LuaSet>();

			List<XElement> invalids = new List<XElement>();
			foreach (XElement element in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				var fe = element.Element(SettingConstants.SettingKeys.FileEncoding);
				var encoding = EncodingName.UTF8;
				if (fe == null)
				{
					element.Add(new XElement(SettingConstants.SettingKeys.FileEncoding, encoding));
				}
				else
				{
					encoding = (EncodingName)Enum.Parse(typeof(EncodingName), fe.Value);
				}

				var wp = string.Empty;
				var workingpath = element.Element(SettingConstants.SettingKeys.WorkingPath);
				if (workingpath == null)
				{
					element.Add(new XElement(SettingConstants.SettingKeys.WorkingPath, ""));
				}
				else
				{
					wp = workingpath.Value;
				}

				LuaSet set = new LuaSet(
					element.Element(SettingConstants.SettingKeys.LuaFolder).Value,
					element.Element(SettingConstants.SettingKeys.LuaExec).Value,
					wp,
					element.Element(SettingConstants.SettingKeys.LuaExecArg).Value,
					encoding
					);

				if (Directory.Exists(set.Folder))
				{
					LuaSettings.Add(element.Element(SettingConstants.SettingKeys.SetName).Value, set);
				}
				else
				{
					invalids.Add(element);
				}
			}

			foreach (XElement element in invalids)
			{
				element.Remove();
			}
			Save();
		}

		public LuaSet GetSetting(string name)
		{
			if (LuaSettings.ContainsKey(name)) return LuaSettings[name];
			else return null;
		}

		public string CurrentSetting
		{
			get
			{
				return XMLLuaSettings.Element(SettingConstants.SettingKeys.CurrentSet).Value;
			}
			set
			{
				XMLLuaSettings.Element(SettingConstants.SettingKeys.CurrentSet).Value = value;
			}
		}

		public void AddSetting(string Name, string Folder, string LuaExecutable, string WorkingPath, string CommandLine, EncodingName Encoding)
		{
			var set = new LuaSet(Folder, LuaExecutable, WorkingPath, CommandLine, Encoding);
			if (LuaSettings.ContainsKey(Name))
			{
				LuaSettings[Name] = set;
			}
			else
			{
				LuaSettings.Add(Name, set);
			}


			XElement element = null;

			foreach (var xl in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				if (xl.Element(SettingConstants.SettingKeys.SetName).Value.Contains(Name))
				{
					element = xl;
					break;
				}
			}

			if (element == null)
			{
				element = new XElement(SettingConstants.SettingKeys.Set);
				element.Add(new XElement(SettingConstants.SettingKeys.SetName, Name));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaFolder, Folder));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaExec, LuaExecutable));
				element.Add(new XElement(SettingConstants.SettingKeys.WorkingPath, WorkingPath));
				element.Add(new XElement(SettingConstants.SettingKeys.LuaExecArg, CommandLine));
				element.Add(new XElement(SettingConstants.SettingKeys.FileEncoding, Encoding));
				XMLLuaSettings.Add(element);
			}
			else
			{
				element.ReplaceNodes(
					new XElement(SettingConstants.SettingKeys.SetName, Name),
					new XElement(SettingConstants.SettingKeys.LuaFolder, Folder),
					new XElement(SettingConstants.SettingKeys.LuaExec, LuaExecutable),
					new XElement(SettingConstants.SettingKeys.WorkingPath, WorkingPath),
					new XElement(SettingConstants.SettingKeys.LuaExecArg, CommandLine),
					new XElement(SettingConstants.SettingKeys.FileEncoding, Encoding)
					);
			}
		}

		public void RemoveSetting(string name)
		{
			XElement element = null;

			foreach (var xl in XMLLuaSettings.Elements(SettingConstants.SettingKeys.Set))
			{
				if (xl.Element(SettingConstants.SettingKeys.SetName).Value.Contains(name))
				{
					element = xl;
					break;
				}
			}

			if (element != null)
			{
				element.Remove();
			}

			if (LuaSettings.ContainsKey(name))
			{
				LuaSettings.Remove(name);
			}
		}

		public bool ContainsSetting(string name)
		{
			return LuaSettings.ContainsKey(name);
		}

		public IEnumerable<string> AllSetting
		{
			get
			{
				return LuaSettings.Keys.ToArray();
			}
		}

		public string GetFirstSettingName()
		{
			string firstName = "";

			if (LuaSettings.Count > 0) firstName = LuaSettings.Keys.First();

			return firstName;
		}
		#endregion

		#region KeyBinding
		public string GetKeyBindingName(string name)
		{
			var set = XMLKeyBinding.Element(name);
			if (set == null)
			{
				System.Collections.ArrayList names = new System.Collections.ArrayList() { };
				if (name == SettingConstants.SettingKeys.KeyBindFolder)
				{
					return GetKeyName(1);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindOutline)
				{
					return GetKeyName(2);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindEditorOutlineRight)
				{
					return GetKeyName(3);
				}
				else if (name == SettingConstants.SettingKeys.KeyBindRunExec)
				{
					return GetKeyName(4);
				}
				return string.Empty;
			}
			else
			{
				KeyBindingSet keySet = new KeyBindingSet(set.Element("Key").Value);
				return keySet.key;
			}
		}
		public void SetBindingKey(string name, string key)
		{
			XElement element = XMLKeyBinding.Element(name);
			if (element == null)
			{
				element = new XElement(name);
				element.Add(new XElement("Key", key));
				XMLKeyBinding.Add(element);
			}
			else
			{
				element.ReplaceNodes(new XElement("Key", key));
			}
		}
		public void RemoveBindingKey(string name)
		{
			var set = XMLKeyBinding.Element(name);
			if (set != null)
			{
				set.Remove();
			}
		}
		public string GetKeyName(int num)
		{
			string name = string.Format("Ctrl+{0}", num);
			return name;
		}
		public IEnumerable<string> AllBindingKey
		{
			get
			{
				List<string> names = new List<string>();

				int i;
				for (i = 1; i <= 4; i++)
				{
					names.Add(GetKeyName(i));
				}

				return names;
			}
		}
		#endregion
	}

	class KeyWordSettings
	{
		public List<KeyValuePair<Color, HashSet<string>>> User { get; private set; }

		public Color Table { get; set; }
		public Color Function { get; set; }

		static KeyWordSettings _instance;
		public static KeyWordSettings Instance
		{
			get
			{
				if (_instance == null) _instance = new KeyWordSettings();
				return _instance;
			}
		}

		private KeyWordSettings()
		{
			string dic = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SettingConstants.SettingFolder);

			var FileName = Path.Combine(dic, SettingConstants.UserKeywordsFile);
			if (File.Exists(FileName))
			{
				var Doc = XDocument.Load(FileName);
				var Users = Doc.Root.Elements(SettingConstants.KeywordsKeys.User);

				User = new List<KeyValuePair<Color, HashSet<string>>>(Users.Count());

				foreach (XElement element in Users)
				{
					var color = Color.FromRgb(
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.R).Value),
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.G).Value),
						byte.Parse(element.Attribute(SettingConstants.KeywordsKeys.B).Value));
					var list = new HashSet<string>();
					foreach (XElement xe in element.Elements())
					{
						if (!string.IsNullOrWhiteSpace(xe.Value))
							list.Add(xe.Value);
					}
					User.Add(new KeyValuePair<Color, HashSet<string>>(color, list));
				}
			}
		}
	}
}
