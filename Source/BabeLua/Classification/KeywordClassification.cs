using Babe.Lua.DataModel;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Babe.Lua.Classification
{
    class HighlightTag
    {
        public static List<Color> UserColors;
		public static Color TableColor;
		public static Color FunctionColor;

        static List<ClassificationTag> UserTags;
		static ClassificationTag LuaTableTag;
		static ClassificationTag LuaFunctionTag;
		static Dictionary<Irony.Parsing.TokenType, ClassificationTag> OtherTags;

        static List<HashSet<string>> Users;

        static bool inited = false;

        public static void InitColors()
        {
            var kw = Babe.Lua.Package.BabePackage.Setting.KeyWords;

			FunctionColor = kw.Function;
			TableColor = kw.Table;

			if (kw.User != null && kw.User.Count > 0)
			{
				UserColors = new List<Color>(kw.User.Count);
				Users = new List<HashSet<string>>(kw.User.Count);
				foreach (var pair in kw.User)
				{
					UserColors.Add(pair.Key);
					Users.Add(pair.Value);
				}
			}
        }

        public static void InitTags(IClassificationTypeRegistryService service)
        {
            if (inited) return;

            InitColors();

			if (UserColors != null && UserColors.Count > 0)
			{
				UserTags = new List<ClassificationTag>(UserColors.Count);

				int count = UserColors.Count;
				if (count > 15) count = 15;
				for (int i = 0; i < count; i++)
				{
					UserTags.Add(new ClassificationTag(service.GetClassificationType(string.Format("UserKeyword{0}", i))));
				}
			}

			OtherTags = new Dictionary<Irony.Parsing.TokenType, ClassificationTag>();
			OtherTags[Irony.Parsing.TokenType.Text] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Character));
			OtherTags[Irony.Parsing.TokenType.Keyword] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Keyword));
			OtherTags[Irony.Parsing.TokenType.Identifier] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Identifier));
			OtherTags[Irony.Parsing.TokenType.String] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.String));
			OtherTags[Irony.Parsing.TokenType.Literal] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Number));
			OtherTags[Irony.Parsing.TokenType.Operator] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Operator));
			OtherTags[Irony.Parsing.TokenType.Comment] = new ClassificationTag(service.GetClassificationType(PredefinedClassificationTypeNames.Comment));

			if (FunctionColor != default(Color))
			{
				LuaFunctionTag = new ClassificationTag(service.GetClassificationType("LuaFunction"));
			}
			else
			{
				LuaFunctionTag = OtherTags[Irony.Parsing.TokenType.Identifier];
			}
			if (TableColor != default(Color))
			{
				LuaTableTag = new ClassificationTag(service.GetClassificationType("LuaTable"));
			}
			else
			{
				LuaTableTag = OtherTags[Irony.Parsing.TokenType.Identifier];
			}

            inited = true;
        }

		static LuaTable m_table = null;
		static bool m_clear = false;
        public static ClassificationTag GetTag(Irony.Parsing.Token token)
        {
			ClassificationTag tag = null;
			if (token.EditorInfo.Type == Irony.Parsing.TokenType.Identifier)
			{
				if (UserTags != null)
				{
					for (int i = 0; i < UserTags.Count; i++)
					{
						if (Users[i].Contains(token.Text)) tag = UserTags[i];
					}
				}

				if (m_table != null)
				{
					var mem = m_table.Members.Find((lm) => { return lm is LuaFunction && lm.Name == token.Text; });
					if (mem != null)
					{
						m_table = null;
						m_clear = false;
						if (tag == null) tag = LuaFunctionTag;
					}
					else if (m_clear == true)
					{
						m_table = null;
						m_clear = false;
					}
					else
					{
						m_clear = true;
					}
				}
				else if (IntellisenseHelper.ContainsFunction(token.Text))
				{
					if (tag == null) tag = LuaFunctionTag;
				}
				else if ((m_table = IntellisenseHelper.GetTable(token.Text)) != null)
				{
					if (tag == null) tag = LuaTableTag;
				}
			}

			if(OtherTags.ContainsKey(token.EditorInfo.Type) && tag == null)
				tag = OtherTags[token.EditorInfo.Type];

			return tag;
        }

		public static ClassificationTag GetTagWithTokenType(Irony.Parsing.TokenType TokenType)
		{
			return OtherTags.ContainsKey(TokenType) ? OtherTags[TokenType] : null;
		}

        #region Register Colors
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LuaTable")]
		static ClassificationTypeDefinition LuaTable = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("LuaFunction")]
		static ClassificationTypeDefinition LuaFunction = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("UserKeyword0")]
        public static ClassificationTypeDefinition UserKeyword0 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword1")]
		public static ClassificationTypeDefinition UserKeyword1 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword2")]
		public static ClassificationTypeDefinition UserKeyword2 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword3")]
		public static ClassificationTypeDefinition UserKeyword3 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword4")]
		public static ClassificationTypeDefinition UserKeyword4 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword5")]
		public static ClassificationTypeDefinition UserKeyword5 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword6")]
		public static ClassificationTypeDefinition UserKeyword6 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword7")]
		public static ClassificationTypeDefinition UserKeyword7 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword8")]
		public static ClassificationTypeDefinition UserKeyword8 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword9")]
		public static ClassificationTypeDefinition UserKeyword9 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword10")]
		public static ClassificationTypeDefinition UserKeyword10 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword11")]
		public static ClassificationTypeDefinition UserKeyword11 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword12")]
		public static ClassificationTypeDefinition UserKeyword12 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword13")]
		public static ClassificationTypeDefinition UserKeyword13 = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("UserKeyword14")]
		public static ClassificationTypeDefinition UserKeyword14 = null;

        //[Export(typeof(ClassificationTypeDefinition))]
        //[Name("UserKeyword15")]
        //public static ClassificationTypeDefinition UserKeyword15 = null;
        #endregion
    }

    #region Register Colors
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LuaTable")]
	[Name("LuaTable")]
	[Order(Before = Priority.Default)]
	public class LuaTableDefinition : ClassificationFormatDefinition
	{
		public LuaTableDefinition()
		{
			this.ForegroundColor = HighlightTag.TableColor;
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "LuaFunction")]
	[Name("LuaFunction")]
	[Order(Before = Priority.Default)]
	public class LuaFunctionDefinition : ClassificationFormatDefinition
	{
		public LuaFunctionDefinition()
		{
			this.ForegroundColor = HighlightTag.FunctionColor;
		}
	}

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "UserKeyword0")]
    [Name("UserKeyword0")]
    [Order(Before = Priority.Default)]
    public class UserKeywordDefinition0 : ClassificationFormatDefinition
    {
        public UserKeywordDefinition0()
        {
            this.ForegroundColor = HighlightTag.UserColors[0];
        }
    }

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword1")]
	[Name("UserKeyword1")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition1 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition1()
		{
			this.ForegroundColor = HighlightTag.UserColors[1];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword2")]
	[Name("UserKeyword2")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition2 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition2()
		{
			this.ForegroundColor = HighlightTag.UserColors[2];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword3")]
	[Name("UserKeyword3")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition3 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition3()
		{
			this.ForegroundColor = HighlightTag.UserColors[3];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword4")]
	[Name("UserKeyword4")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition4 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition4()
		{
			this.ForegroundColor = HighlightTag.UserColors[4];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword5")]
	[Name("UserKeyword5")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition5 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition5()
		{
			this.ForegroundColor = HighlightTag.UserColors[5];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword6")]
	[Name("UserKeyword6")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition6 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition6()
		{
			this.ForegroundColor = HighlightTag.UserColors[6];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword7")]
	[Name("UserKeyword7")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition7 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition7()
		{
			this.ForegroundColor = HighlightTag.UserColors[7];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword8")]
	[Name("UserKeyword8")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition8 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition8()
		{
			this.ForegroundColor = HighlightTag.UserColors[8];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword9")]
	[Name("UserKeyword9")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition9 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition9()
		{
			this.ForegroundColor = HighlightTag.UserColors[9];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword10")]
	[Name("UserKeyword10")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition10 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition10()
		{
			this.ForegroundColor = HighlightTag.UserColors[10];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword11")]
	[Name("UserKeyword11")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition11 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition11()
		{
			this.ForegroundColor = HighlightTag.UserColors[11];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword12")]
	[Name("UserKeyword12")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition12 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition12()
		{
			this.ForegroundColor = HighlightTag.UserColors[12];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword13")]
	[Name("UserKeyword13")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition13 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition13()
		{
			this.ForegroundColor = HighlightTag.UserColors[13];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "UserKeyword14")]
	[Name("UserKeyword14")]
	[Order(Before = Priority.Default)]
	public class UserKeywordDefinition14 : ClassificationFormatDefinition
	{
		public UserKeywordDefinition14()
		{
			this.ForegroundColor = HighlightTag.UserColors[14];
		}
	}

    //[Export(typeof(EditorFormatDefinition))]
    //[ClassificationType(ClassificationTypeNames = "UserKeyword15")]
    //[Name("UserKeyword15")]
    //[Order(Before = Priority.Default)]
    //public class UserKeywordDefinition15 : ClassificationFormatDefinition
    //{
    //    public UserKeywordDefinition15()
    //    {
    //        this.ForegroundColor = KeywordClassification.UserColors[15];
    //    }
    //}
#endregion
}
