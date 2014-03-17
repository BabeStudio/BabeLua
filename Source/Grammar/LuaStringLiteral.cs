using System;
using System.Collections.Generic;
using Irony.Parsing;
namespace Grammar
{
    class LuaStringLiteral : StringLiteral
    {
        public LuaStringLiteral(string name)
            : base(name)
        {
            this.AddStartEnd("'", StringOptions.AllowsAllEscapes);
            this.AddStartEnd("\"", StringOptions.AllowsAllEscapes);
        }

        protected override string HandleSpecialEscape(string segment, CompoundTokenDetails details)
        {
            if (string.IsNullOrEmpty(segment)) return string.Empty;
            char first = segment[0];
            switch (first)
            {
                case 'a':
                case 'b':
                case 'f': 
                case 'n':
                case 'r':
                case 't': 
                case 'v':
                case '\\': 
                case '"': 
                case '\'': 
                    break;

                case '0':
                case '1':
                case '2':
                    {
                        bool success = false;
                        if (segment.Length >=3)
                        {
                            string value = segment.Substring(0, 3);
                            int dummy = 0;
                            success = Int32.TryParse(value, out dummy);
                        
                        }
                        
                        if(!success)
                            details.Error = "Invalid escape sequence: \000 must be a valid number.";

                    }
                    break;
            }
            details.Error = "Invalid escape sequence: \\" + segment;
            return segment;
        }
    }
} 