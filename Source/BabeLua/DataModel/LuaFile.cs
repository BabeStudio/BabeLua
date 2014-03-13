using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;

namespace LuaLanguage.DataModel
{
    class LuaFile:IEquatable<LuaFile>
    {
        public string File { get; private set; }
        public List<LuaMember> Members { get; private set; }
        public TokenList Tokens { get; private set; }
 
        public LuaFile(string file, TokenList tokens) 
        {
            this.File = file;
            this.Tokens = tokens;
            Members = new List<LuaMember>();
        }

        public void AddTable(LuaTable table)
        {
            this.Members.Add(table);
        }

        public LuaTable GetTable(string name)
        {
            for(int i = 0;i<Members.Count;i++)
            {
                if(Members[i].Name.Equals(name)) return Members[i] as LuaTable;
            }
            return null;
        }

        public bool ContainsTable(string table)
        {
            foreach (LuaMember lt in Members)
            {
                if (lt is LuaTable)
                {
                    if (lt.Name.Equals(table)) return true;
                }
            }
            return false;
        }

        public bool ContainsFunction(string function)
        {
            foreach (LuaMember lf in Members)
            {
                if (lf is LuaFunction)
                {
                    if (lf.Name.Equals(function)) return true;
                }
            }
            
            return false;
        }

        public bool Equals(LuaFile other)
        {
            return this.File.Equals(other.File);
        }

        public override string ToString()
        {
            return System.IO.Path.GetFileName(File);
        }
    }
}
