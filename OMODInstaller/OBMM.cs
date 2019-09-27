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
    }
}
