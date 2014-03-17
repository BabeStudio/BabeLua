using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.DataModel
{
    class LuaMember : IEquatable<LuaMember>
    {
        public string Name { get; protected set; }

        //所在的位置
        public LuaFile File { get; set; }
		public int Line { get; protected set; }
		public int Column { get; protected set; }
		
        //Preview只用于查找引用的用途
		public string Preview { get; set; }

        public LuaMember(string name, int line, int column)
        {
            this.Name = name;
            this.Line = line;
            this.Column = column;
        }

        public LuaMember(Token token)
        {
            this.Name = token.ValueString;
            this.Line = token.Location.Line;
            this.Column = token.Location.Column;
        }

        public bool Equals(LuaMember other)
        {
            return other.Name.Equals(this.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public LuaMember Copy()
        {
            LuaMember mem = new LuaMember(this.Name, this.Line, this.Column);
            return mem;
        }
    }
}
