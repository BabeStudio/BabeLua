using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.DataModel
{
    class LuaInnerTable : LuaFile
    {
        static LuaInnerTable instance;
        public static LuaInnerTable Instance
        {
            get
            {
                if (instance == null) instance = new LuaInnerTable();
                return instance;
            }
        }

        private LuaInnerTable():base(null,null)
        {
            string[] global = { "assert", "collectgarbage", "dofile", "error", "getfenv", "getmetatable", "ipairs", "load", "loadfile", "loadstring", "module", "next", "pairs", "pcall", "print", "rawequal", "rawget", "rawset", "require", "select", "setfenv", "setmetatable", "tonumber", "tostring", "type", "unpack", "xpcall", "class", "new", "delete" };
            foreach (var str in global)
            {
                this.Members.Add(new LuaFunction(str, -1, null));
            }

            var tbs = new Dictionary<string, HashSet<string>>();

            tbs["coroutine"] = new HashSet<string>() { "create", "resume", "running", "status", "wrap", "yield", };
            tbs["debug"] = new HashSet<string>() { "debug", "getfenv", "gethook", "getinfo", "getlocal", "getmetatable", "getregistry", "getupvalue", "setfenv", "sethook", "setlocal", "setmetatable", "setupvalue", "traceback", };
            tbs["io"] = new HashSet<string>() { "close", "flush", "input", "lines", "open", "output", "popen", "read", "stderr", "stdin", "stdout", "tmpfile", "type", "write", };
            tbs["math"] = new HashSet<string>() { "abs", "acos", "asin", "atan", "atan2", "ceil", "cos", "cosh", "deg", "exp", "floor", "fmod", "frexp", "huge", "ldexp", "log", "log10", "max", "min", "modf", "pi", "pow", "rad", "random", "randomseed", "sin", "sinh", "sqrt", "tan", "tanh", };
            tbs["os"] = new HashSet<string>() { "clock", "date", "difftime", "execute", "exit", "getenv", "remove", "rename", "setlocale", "time", "tmpname", };
            tbs["package"] = new HashSet<string>() { "cpath", "loaded", "loaders", "loadlib", "path", "preload", "seeall", };
            tbs["string"] = new HashSet<string>() { "byte", "char", "dump", "find", "format", "gmatch", "gsub", "len", "lower", "match", "rep", "reverse", "sub", "upper", };
            tbs["table"] = new HashSet<string>() { "concat", "sort", "maxn", "remove", "insert", };

            foreach (var tb in tbs)
            {
                LuaTable lt = new LuaTable(tb.Key, -1);
                foreach (var st in tb.Value)
                {
                    lt.AddFunction(new LuaFunction(st, -1, null));
                }
                this.AddTable(lt);
            }

            string[] keywords = {"and","break","do","else","elseif",
                                "end","false","for","function","if",
                                "in","local","nil","not","or",
                                "repeat","return","then","true","until","while"};
            foreach (var str in keywords)
            {
                this.Members.Add(new LuaMember(str,-1,-1));
            }
        }

        public bool ContainsTableFunction(string table, string func)
        {
            foreach (LuaMember m in Members)
            {
                if (m is LuaTable && m.Name.Equals(table))
                {
                    foreach (LuaMember f in (m as LuaTable).Members)
                    {
                        if (f.Name.Equals(func)) return true;
                    }
                }
            }
            return false;
        }
    }
}
