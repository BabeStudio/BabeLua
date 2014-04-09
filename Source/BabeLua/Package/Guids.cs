// Guids.cs
// MUST match guids.h
using System;

namespace Babe.Lua.Package
{
    static class GuidList
    {
        public const string PkgString = "ef2b9b1b-2d16-4088-95d9-904637d77b19";
        public const string CmdString = "f05504a2-fa3b-4cef-83b2-2a16ca67a7c8";
        public const string SearchWindowString1 = "285E3040-39CF-48E6-A4C1-21407730434B";
        public const string SearchWindowString2 = "A058B23C-62C6-4848-9E16-44B4A3A3816B";
        public const string OutlineWindowString = "991ACD51-940E-41AF-AE45-E7C44FC95436";
        public const string FolderWindowString = "E3CBD377-FB74-4068-B28D-79CEBFCE7675";
        public const string SettingWindowString = "BA1C6664-51A4-4C1F-9636-2BE22099EA9F";

        public static readonly Guid CmdSetString = new Guid(CmdString);
    };
}