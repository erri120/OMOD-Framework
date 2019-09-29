using System;
using System.IO;
namespace OMODFramework
{
    public class Framework
    {
        internal string OBMMFakeVersion = "1.1.12";
        internal byte OBMMFakeMajorVersion = 1;
        internal byte OBMMFakeMinorVersion = 1;
        internal byte OBMMFakeBuildNumber = 12;

        internal static string TempDir { get; set; } = Path.GetTempPath() + @"obmm\";
        internal static string OblivionDir { get; set; }
        internal static string DataDir { get; set; }
        internal static string OblivionINIPath { get; set; }
        internal static string OblivionESPPath { get; set; }
        internal static string OutputDir { get; set; }

        internal string CorrectPath(string path) { return path.Trim().EndsWith("\\") ? path : path += "\\"; }

        #region API

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
        public void SetOutputDirectory(string path) { OutputDir = CorrectPath(path); }

        /// <summary>
        /// Sets the internal path to the plugins.txt file normally found in the AppData/Oblivion folder,
        /// needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the plugins.txt file</param>
        public void SetPluginsListPath(string path) { OblivionESPPath = CorrectPath(path); }

        /// <summary>
        /// Sets the internal path to the Oblivion.ini file normally found in the My Games folder,
        /// needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion.ini file</param>
        public void SetOblivionINIPath(string path) { OblivionINIPath = CorrectPath(path); }

        /// <summary>
        /// Sets the internal path to the Oblivion game folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion game folder</param>
        public void SetOblivionDirectory(string path) { OblivionDir = CorrectPath(path); }

        /// <summary>
        /// Sets the internal path to the Oblivion data folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the Oblivion data folder</param>
        public void SetDataDirectory(string path) { DataDir = CorrectPath(path); }

        /// <summary>
        /// Sets the internal path to the temp folder, needs to be an absolute path
        /// </summary>
        /// <param name="path">Path to the temp folder</param>
        public void SetTempDirectory(string path) { TempDir = CorrectPath(path); }

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

        #endregion
    }
}
