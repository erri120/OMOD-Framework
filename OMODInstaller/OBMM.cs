using System;
using System.IO;
using System.Windows.Forms;

namespace OblivionModManager
{
    internal static class Program
    {
        internal const string version = "1.1.12"; // latest official obmm release
        internal const byte MajorVersion = 1;
        internal const byte MinorVersion = 1;
        internal const byte BuildNumber = 12;
        internal const byte CurrentOmodVersion = 4;

        internal static string TempDir
        {
            get { return Path.GetTempPath() + @"obmm\"; }
        }

        internal static readonly string CurrentDir = (Path.GetDirectoryName(Application.ExecutablePath) + "\\").ToLower();
        internal static readonly string OblivionINIDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My games\\oblivion\\");
        internal static readonly string OblivionESPDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oblivion\\");

        internal static sData Data;

        internal static bool IsSafeFileName(string s)
        {
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[s.Length - 1]) != -1) return false;
            return true;
        }

        internal static bool IsSafeFolderName(string s)
        {
            if (s.Length == 0) return true;
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".")) return false;
            return true;
        }
    }
}
