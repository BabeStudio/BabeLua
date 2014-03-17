using Babe.Lua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua.Package;
using Grammar;

namespace Babe.Lua.DataModel
{
	class FileManager
	{
		public List<LuaFile> Files { get; private set; }

		public List<LuaMember> CurrentFileToken { get; private set; }

		int _current = -1;
		public LuaFile CurrentFile
		{
			get
			{
				if (_current != -1) return Files[_current];
				else return null;
			}
			set
			{
				if (_current != -1 && value != null)
				{
					Files[_current] = value;
					RefreshCurrentFile();
				}
				else
				{
					_current = -1;
				}
			}
		}

		static FileManager _instance;
		public static FileManager Instance
		{
			get
			{
				if (_instance == null) _instance = new FileManager();
				return _instance;
			}
		}

		private FileManager()
		{
			Files = new List<LuaFile>();
			CurrentFileToken = new List<LuaMember>();
		}

		public void ClearData()
		{
			Files.Clear();
			CurrentFileToken.Clear();
			_current = -1;
		}

		public void AddFile(LuaFile file)
		{
			var index = Files.IndexOf(file);
			if (index != -1) Files[index] = file;
			else
			{
				Files.Add(file);
			}
		}

		public void SetActiveFile(string file)
		{
			for (int i = 0; i < Files.Count; i++)
			{
				if (file.Equals(Files[i].File))
				{
					_current = i;

					RefreshCurrentFile();

					return;
				}
			}
		}

		public void RefreshCurrentFile()
		{
			if (CurrentFile != null)
			{
				CurrentFileToken = new List<LuaMember>();
				foreach (var token in CurrentFile.Tokens)
				{
					if (token.Category == Irony.Parsing.TokenCategory.Content && token.Terminal.Name == LuaTerminalNames.Identifier)
						CurrentFileToken.Add(new LuaMember(token));
				}
			}
		}

		public List<LuaMember> GetAllGlobals()
		{
			List<LuaMember> list = new List<LuaMember>();
			for (int i = 0; i < Files.Count; i++)
			{
				list.AddRange(Files[i].Members);
			}
			return list;
		}

		public List<LuaMember> FindRefInFile(LuaFile file, string keyword)
		{
			List<LuaMember> members = new List<LuaMember>();

			if (file != null)
			{
				try
				{
					System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(keyword));
					
					List<string> lines = new List<string>();
					using (StreamReader sr = new StreamReader(file.File))
					{
						while (sr.Peek() >= 0)
						{
							lines.Add(sr.ReadLine());
						}
					}

					for (int i = 0; i < lines.Count; i++)
					{
						var match = reg.Match(lines[i]);
						while (match.Success)
						{
							var lm = new LuaMember(keyword, i, match.Index);
							lm.Preview = lines[i];
							lm.File = file;
							members.Add(lm);
							match = match.NextMatch();
						}
					}

					//int index = -1;
					//for(int i = 0;i<lines.Count;i++)
					//{
					//	index = lines[i].IndexOf(keyword);
					//	while (index != -1)
					//	{
					//		var lm = new LuaMember(keyword, i, index);
					//		lm.Preview = lines[i];
					//		lm.File = file.File;
					//		members.Add(lm);
					//		index = lines[i].IndexOf(keyword, index + keyword.Length);
					//	}
					//}

					//foreach (var token in file.Tokens)
					//{
					//	if (token.EditorInfo == null || token.EditorInfo.Type != Irony.Parsing.TokenType.String)
					//	{
					//		if (token.ValueString.Equals(keyword))
					//		{
					//			var lmp = new LuaMember(token);
					//			lmp.Preview = lines[lmp.Line];
					//			lmp.File = file.File;
					//			members.Add(lmp);
					//		}
					//	}
					//	else
					//	{
					//		if (string.IsNullOrEmpty(token.ValueString)) continue;
					//		int index = token.ValueString.IndexOf(keyword);
					//		if (index != -1)
					//		{

					//			var lmp = new LuaMember(keyword, token.Location.Line, token.Location.Column + 1 + index);
					//			lmp.Preview = lines[lmp.Line];
					//			lmp.File = file.File;
					//			members.Add(lmp);

					//		}
					//	}
					//}
				}
				catch { }
			}

			return members;
		}

		public List<LuaMember> FindAllRef(string keyword, bool AllFile)
		{
			List<LuaMember> results = new List<LuaMember>();
			if (!AllFile)
			{
				//var result = Babe.Lua.TextMarker.FindWordTaggerProvider.CurrentTagger.SearchText(keyword);
				//foreach (var span in result)
				//{
				//	var line = span.Start.GetContainingLine();
				//	var lm = new LuaMember(keyword, line.LineNumber, span.Start - line.Start);
				//	lm.File = DTEHelper.Current.DTE.ActiveDocument.FullName;
				//	lm.Preview = line.GetText();
				//	results.Add(lm);
				//}
				DTEHelper.Current.DTE.ActiveDocument.Save();
				results = FindRefInFile(CurrentFile, keyword);
			}
			else
			{
				DTEHelper.Current.DTE.Documents.SaveAll();
				//bool saved = false;
				//while (!saved)
				//{
				//	saved = true;
				//	foreach (EnvDTE.Document doc in DTEHelper.Current.DTE.Documents)
				//	{
				//		if (!doc.Saved)
				//		{
				//			saved = false;
				//			break;
				//		}
				//	}
				//}

				//List<LuaMember> members = new List<LuaMember>();

				//HashSet<string> OpenFiles = new HashSet<string>();
				//foreach (EnvDTE.Document doc in DTEHelper.Current.DTE.Documents)
				//{
				//	OpenFiles.Add(doc.FullName);

					
				//}

				for (int i = 0; i < Files.Count; i++)
				{
					//if (OpenFiles.Contains(Files[i].File)) continue;
					results.AddRange(FindRefInFile(Files[i], keyword));
				}
			}

			return results;
		}

		public List<LuaMember> FindDefination(string keyword)
		{
			List<LuaMember> members = new List<LuaMember>();

			string[] keywords = keyword.Split(new char[] { '.', ':' }, StringSplitOptions.RemoveEmptyEntries);
			if (keywords.Length == 0) return null;

			for (int i = 0; i < Files.Count; i++)
			{
				try
				{
					List<string> lines = new List<string>();
					using (StreamReader sr = new StreamReader(Files[i].File))
					{
						while (sr.Peek() >= 0)
						{
							lines.Add(sr.ReadLine());
						}
					}

					foreach (LuaMember member in Files[i].Members)
					{
						if (member is LuaTable)
						{
							foreach (LuaMember lm in (member as LuaTable).Members)
							{
								if (lm.Name.Equals(keyword))
								{
									var lmp = lm.Copy();
									lmp.Preview = lines[lm.Line];
									lmp.File = Files[i];
									members.Add(lmp);
								}
							}
						}

						if (member.Name.Equals(keyword))
						{
							var lmp = member.Copy();
							lmp.Preview = lines[member.Line];
							lmp.File = Files[i];
							members.Add(lmp);
						}
					}
				}
				catch { }
			}

			return members;
		}

		public IEnumerable<LuaMember> FindRefInFile2(LuaFile file, string keyword)
		{
			if (file != null)
			{
				List<string> lines = new List<string>();
				try
				{
					using (StreamReader sr = new StreamReader(file.File))
					{
						while (sr.Peek() >= 0)
						{
							lines.Add(sr.ReadLine());
						}
					}
				}
				catch { }

				System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(keyword);

				for (int i = 0; i < lines.Count; i++)
				{
					var match = reg.Match(lines[i]);
					while (match.Success)
					{
						var lm = new LuaMember(keyword, i, match.Index);
						lm.Preview = lines[i];
						lm.File = file;
						yield return lm;
						match = match.NextMatch();
					}
				}
			}
		}

		public IEnumerable<LuaMember> FindRef(string keyword, bool AllFile)
		{
			if (!AllFile)
			{
				DTEHelper.Current.DTE.ActiveDocument.Save();
				return FindRefInFile2(CurrentFile, keyword);
			}
			else
			{
				List<LuaMember> results = new List<LuaMember>();

				DTEHelper.Current.DTE.Documents.SaveAll();

				for (int i = 0; i < Files.Count; i++)
				{
					results.AddRange(FindRefInFile2(Files[i], keyword));
				}

				return results;
			}
		}
	}
}
