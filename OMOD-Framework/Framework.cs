using System;
using System.IO;
using System.Collections.Generic;
namespace OMODFramework
{
    public class Framework
    {
        #region Internal Variables
        internal string OBMMFakeVersion = "1.1.12";
        internal byte OBMMFakeMajorVersion = 1;
        internal byte OBMMFakeMinorVersion = 1;
        internal byte OBMMFakeBuildNumber = 12;
        internal byte OBMMFakeCurrentOmodVersion = 4;

        internal static string DLLPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "erri120.OMODFramework.dll");
        internal static string TempDir { get; set; } = Path.Combine(Path.GetTempPath(), "obmm");
        internal static string OblivionDir { get; set; }
        internal static string DataDir { get; set; }
        internal static string OblivionINIPath { get; set; }
        internal static string OblivionESPPath { get; set; }
        internal static string OutputDir { get; set; }
        #endregion

        #region OBMM Functions
        internal static bool IsSafeFileName(string s)
        {
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[s.Length - 1]) != -1) return false;
            return true;
        }
        internal static bool IsSafeFolderName(string s)
        {
            if (s.Length == 0) return true;
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".")) return false;
            return true;
        }
        internal static bool strArrayContains(List<string> a, string s)
        {
            s = s.ToLower();
            foreach (string s2 in a)
            {
                if (s2.ToLower() == s) return true;
            }
            return false;
        }
        /*
        internal static bool strArrayContains(List<DataFileInfo> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].LowerFileName == s) return true;
            }
            return false;
        }*/
        /*
        internal static DataFileInfo strArrayGet(DataFileInfo[] a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].LowerFileName == s) return a[i];
            }
            return null;
        }*/
        internal static void strArrayRemove(List<string> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].ToLower() == s)
                {
                    a.RemoveAt(i);
                    return;
                }
            }
        }
        /*
        internal static void strArrayRemove(List<DataFileInfo> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].LowerFileName == s)
                {
                    a.RemoveAt(i);
                    return;
                }
            }
        }*/
        internal static FileStream CreateTempFile() { return CreateTempFile(out string s); }
        internal static FileStream CreateTempFile(out string path)
        {
            for(int i = 0; i < 32000; i++)
            {
                if (!File.Exists(Path.Combine(TempDir,"tmp_" + i.ToString())))
                {
                    path = Path.Combine(TempDir, "tmp_" + i.ToString());
                    return File.Create(path);
                }
            }
            throw new Exception("Could not create a new temp file because the directory is full!");
        }
        internal static string CreateTempDirectory()
        {
            for (int i = 0; i < 32000; i++)
            {
                if (!Directory.Exists(Path.Combine(TempDir + i.ToString())))
                {
                    Directory.CreateDirectory(Path.Combine(TempDir,i.ToString()));
                    return Path.Combine(TempDir,i.ToString());
                }
            }
            throw new Exception("Could not create a new temp folder because directory is full!");
        }
        internal static void ClearTempFiles() { ClearTempFiles(""); }
        internal static void ClearTempFiles(string subfolder)
        {
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
            if (!Directory.Exists(Path.Combine(TempDir, subfolder))) return;
            foreach (string file in Directory.GetFiles(Path.Combine(TempDir, subfolder)))
            {
                try { File.Delete(file); } catch { }
            }
            try { Directory.Delete(Path.Combine(TempDir, subfolder), true); } catch { }
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
        }
        #endregion

        #region API Functions

        /// <summary>
        /// Sets all internal variables if you don't want to call each setter separately,
        /// all paths have to be absolute paths
        /// </summary>
        /// <param name="version">Syntax: "Major.Minor.Build"</param>
        /// <param name="oblivionPath">Path to the Oblivion game folder</param>
        /// <param name="oblivionDataPath">Path to the Oblivion data folder</param>
        /// <param name="oblivionINIPath">Path to the Oblivion.ini file</param>
        /// <param name="oblivionPluginsPath">Path to the plugins.txt file</param>
        /// <param name="outputPath">Path to the Output folder</param>
        /// <param name="tempPath">Path to the Temp folder</param>
        public void Setup(string version, string oblivionPath, string oblivionDataPath, string oblivionINIPath,
            string oblivionPluginsPath, string outputPath, string tempPath)
        {
            Byte.TryParse(version.Split('.')[0], out byte major);
            Byte.TryParse(version.Split('.')[1], out byte minor);
            Byte.TryParse(version.Split('.')[2], out byte build);
            SetOBMMVersion(major, minor, build);
            SetOblivionDirectory(oblivionPath);
            SetDataDirectory(oblivionDataPath);
            SetOblivionINIPath(oblivionINIPath);
            SetPluginsListPath(oblivionPluginsPath);
            SetOutputDirectory(outputPath);
            SetTempDirectory(tempPath);
        }

        /// <summary>
        /// Sets the internal path to the output directory of any write action, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the output folder</param>
        public void SetOutputDirectory(string path) { OutputDir = path; }

        /// <summary>
        /// Sets the internal path to the plugins.txt file normally found in the AppData/Oblivion folder,
        /// needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the plugins.txt file</param>
        public void SetPluginsListPath(string path) { OblivionESPPath = path; }

        /// <summary>
        /// Sets the internal path to the Oblivion.ini file normally found in the My Games folder,
        /// needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion.ini file</param>
        public void SetOblivionINIPath(string path) { OblivionINIPath = path; }

        /// <summary>
        /// Sets the internal path to the Oblivion game folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion game folder</param>
        public void SetOblivionDirectory(string path) { OblivionDir = path; }

        /// <summary>
        /// Sets the internal path to the Oblivion data folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion data folder</param>
        public void SetDataDirectory(string path) { DataDir = path; }

        /// <summary>
        /// Sets the internal path to the temp folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the temp folder</param>
        public void SetTempDirectory(string path) { TempDir = path; }

        /// <summary>
        /// Sets the internal fake version of OBMM, useful when mod needs a certain version
        /// </summary>
        /// <param name="major">The major verison</param>
        /// <param name="minor">The minor version</param>
        /// <param name="build">The build number</param>
        public void SetOBMMVersion(byte major, byte minor, byte build)
        {
            OBMMFakeVersion = $"{major}.{minor}.{build}";
            OBMMFakeMajorVersion = major;
            OBMMFakeMinorVersion = minor;
            OBMMFakeBuildNumber = build;
        }

        /// <summary>
        /// Sets the internal path to the dll, useful when debugging
        /// </summary>
        /// <param name="path">Path to the dll</param>
        public void SetDLLPath(string path) { DLLPath = path; }

        #endregion
    }
}
