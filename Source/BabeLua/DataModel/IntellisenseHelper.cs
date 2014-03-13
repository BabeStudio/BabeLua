using Babe.Lua;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LuaLanguage.DataModel
{
    /// <summary>
    /// 此对象需要项目间隔离
    /// </summary>
    class IntellisenseHelper
    {
        static FileManager FileManager = FileManager.Instance;
        static LuaInnerTable InnerTables = LuaInnerTable.Instance;

        static bool isScanning = false;
        static bool isBreak = false;
        static Thread _thread;

        public static void Scan()
        {
            var set = BabePackage.Current.CurrentSetting;

            if (set == null || string.IsNullOrWhiteSpace(set.Folder) || !Directory.Exists(set.Folder))
            {
                //FileManager.ClearData();
                return;
            }

            if (isScanning)
            {
                isBreak = true;
                // Use the Join method to block the current thread 
                // until the object's thread terminates.
                if (_thread != null && _thread.IsAlive)
                    _thread.Join();
            }

            _thread = new Thread(Work);
            _thread.Start();
        }

        static void Work()
        {
            FileManager.ClearData();

            isScanning = true;
            isBreak = false;

			var names = System.IO.Directory.GetFiles(BabePackage.Current.CurrentSetting.Folder, "*.lua", System.IO.SearchOption.AllDirectories).Where((name) => { return name.ToLower().EndsWith(".lua"); });

            int count = 0;

            foreach (var name in names)
            {
                if (isBreak)
                {
                    isBreak = false;
                    return;
                }
                var tp = new TreeParser();
                tp.HandleFile(name);
                DTEHelper.Current.GetStatusBar().Progress(true, "scan", ++count, names.Count());
            }

            isScanning = false;
            DTEHelper.Current.GetStatusBar().Progress(false);

            DTEHelper.Current.RefreshOutlineWnd();
        }

        public static void Refresh(Irony.Parsing.ParseTree tree)
        {
            if (isScanning) return;
            var file = FileManager.CurrentFile;
            if (file == null || DTEHelper.Current.DTE.ActiveDocument == null || file.File != DTEHelper.Current.DTE.ActiveDocument.FullName) return;

            if (System.IO.File.Exists(file.File))
            {
                var tp = new TreeParser();
                tp.Refresh(tree);

                DTEHelper.Current.RefreshEditorOutline();
                DTEHelper.Current.RefreshOutlineWnd();
            }
            else
            {
                //文件已经被移除
                IntellisenseHelper.RemoveFile(file.File);
                FileManager.CurrentFile = null;
            }
        }

        public static void SetFile(string file)
        {
            var tp = new TreeParser();
            tp.HandleFile(file);
            FileManager.Instance.SetActiveFile(file);
            System.Diagnostics.Debug.Print("Current File is : " + file);
        }

        public static void RemoveFile(string file)
        {
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].File == file)
                {
                    FileManager.Files.RemoveAt(i);
                    break;
                }
            }
        }

        public static bool ContainsTable(string table)
        {
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].ContainsTable(table)) return true;
            }

            return InnerTables.ContainsTable(table);
        }

        public static bool ContainsFunction(string function)
        {
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                if (FileManager.Files[i].ContainsFunction(function)) return true;
            }

            return InnerTables.ContainsFunction(function);
        }

        public static LuaTable GetTable(string table)
        {
            LuaTable lt = null;
            for (int i = 0; i < FileManager.Files.Count; i++)
            {
                lt = FileManager.Files[i].GetTable(table);
                if (lt != null) return lt;
            }
            return InnerTables.GetTable(table);
        }

        public static IEnumerable<LuaMember> GetGlobal()
        {
            var list = FileManager.GetAllGlobals();
            list.AddRange(InnerTables.Members);
            return list.Distinct();
        }

        public static IEnumerable<LuaMember> GetFileTokens()
        {
            return FileManager.CurrentFileToken.Distinct();
        }
    }
}
