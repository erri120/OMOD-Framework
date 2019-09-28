using System;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OblivionModManager.Scripting
{
    internal class ScriptFunctions : IScriptFunctions
    {
        private readonly System.Security.PermissionSet permissions;
        private readonly ScriptReturnData srd;
        private readonly string DataFiles;
        private readonly string Plugins;
        private readonly string[] dataFileList;
        private readonly string[] pluginList;
        private readonly string[] dataFolderList;
        private readonly string[] pluginFolderList;
        private readonly bool testMode; //ignore

        internal ScriptFunctions(ScriptReturnData srd, string dataFilesPath, string pluginsPath)
        {
            this.srd = srd;
            DataFiles = dataFilesPath;
            Plugins = pluginsPath;

            permissions = new PermissionSet(PermissionState.None);
            List<string> paths = new List<string>(4);

            paths.Add(Program.CurrentDir);
            paths.Add(Program.OblivionINIDir);
            if (dataFilesPath != null) paths.Add(dataFilesPath);
            if (pluginsPath != null) paths.Add(pluginsPath);

            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, paths.ToArray()));
            permissions.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));

            testMode = false;
        }

        internal ScriptFunctions(ScriptReturnData srd, string[] dataFiles, string[] plugins)
        {
            this.srd = srd;
            dataFileList = (string[])dataFiles.Clone();
            pluginList = (string[])plugins.Clone();

            //temp
            List<string> df = new List<string>();
            string dir;

            df.Add("");
            for (int i = 0; i < dataFileList.Length; i++)
            {
                dataFileList[i] = dataFileList[i].ToLower();
                dir = dataFileList[i];
                while (dir.Contains(@"\"))
                {
                    dir = Path.GetDirectoryName(dir);
                    if (dir != null && dir != "")
                    {
                        if (!df.Contains(dir)) df.Add(dir);
                    }
                    else break;
                }
            }
            dataFolderList = df.ToArray();

            df.Clear();
            df.Add("");
            for(int i = 0; i < pluginList.Length; i++)
            {
                pluginList[i] = pluginList[i].ToLower();
                dir = pluginList[i];
                while (dir.Contains(@"\"))
                {
                    dir = Path.GetDirectoryName(dir);
                    if (dir != null && dir != "")
                    {
                        if (!df.Contains(dir)) df.Add(dir);
                    }
                    else break;
                }
            }
            pluginFolderList = df.ToArray();

            string[] paths = new string[2];
            paths[0] = Program.CurrentDir;
            paths[1] = Program.OblivionINIDir;
            permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, paths));
            permissions.AddPermission(new UIPermission(UIPermissionWindow.AllWindows));
            testMode = true;
        }

        private bool ExistsIn(string path, string[] files)
        {
            if (files == null) return false;
            return Array.Exists(files, new Predicate<string>(path.ToLower().Equals));
        }

        private void CheckPathSafety(string path)
        {
            if(!Program.IsSafeFileName(path)) throw new Exception("Illegal file name: "+path);
        }

        private void CheckPluginSafety(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, pluginList) : File.Exists(Plugins + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckDataSafety(string path)
        {
            permissions.Assert();
            if (!Program.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path);
            if (!(testMode ? ExistsIn(path, dataFileList) : File.Exists(DataFiles + path))) throw new Exception("File " + path + " not found");
        }

        private void CheckFolderSafety(string path)
        {
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
        }

        private void CheckPluginFolderSafety(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, pluginFolderList) : Directory.Exists(Plugins + path))) throw new ScriptingException("Folder " + path + " not found");
        }

        private void CheckDataFolderSafety(string path)
        {
            permissions.Assert();
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Program.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
            if (!(testMode ? ExistsIn(path, dataFolderList) : Directory.Exists(DataFiles + path))) throw new Exception("Folder " + path + " not found");
        }

        // Looks sick but is just used to see what files will be affected and only called when testmode is true
        // dunno if actually useful maybe delete later
        private string[] SimulateFSOutput(string[] fsList, string path, string pattern, bool recurse)
        {
            pattern = "^" + (pattern == "" ? ".*" : pattern.Replace("[", @"\[").Replace(@"\", "\\").Replace("^", @"\^").Replace("$", @"\$").
                Replace("|", @"\|").Replace("+", @"\+").Replace("(", @"\(").Replace(")", @"\)").
                Replace(".", @"\.").Replace("*", ".*").Replace("?", ".{0,1}")) + "$";
            return Array.FindAll(fsList, delegate (string value)
            {
                if ((path.Length > 0 && value.StartsWith(path.ToLower() + @"\")) || path.Length == 0)
                {
                    if (value == "" || (!recurse && Regex.Matches(value.Substring(path.Length), @"\\", RegexOptions.None).Count > 1)) return false;
                    if (Regex.IsMatch(value.Substring(value.LastIndexOf('\\') + 1), pattern)) return true;
                }
                return false;
            });
        }

        /// <summary>
        /// Returns all files within a folder that match the pattern
        /// </summary>
        /// <param name="path">Path to the folder</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="recurse">To check for only the top-level directory or sub-directories</param>
        /// <returns></returns>
        private string[] GetFilePaths(string path, string pattern, bool recurse)
        {
            permissions.Assert();
            return Directory.GetFiles(path, (pattern != "" && pattern != null) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Returns all directories within a folder that match the pattern
        /// </summary>
        /// <param name="path">Path to the folder</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="recurse">To check for only the top-level directory or sub-directories</param>
        /// <returns></returns>
        private string[] GetDirectoryPaths(string path, string pattern, bool recurse)
        {
            permissions.Assert();
            return Directory.GetDirectories(path, (pattern != "" && pattern != null) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Removes the rooted element of all paths in an array (rooted meaning C:dir, /dir or C:/dir)
        /// </summary>
        /// <param name="paths">Array of paths to strip</param>
        /// <param name="baseLength">The position where the new path starts</param>
        /// <returns></returns>
        private string[] StripPathList(string[] paths, int baseLength)
        {
            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(baseLength);
            return paths;
        }

        public void CancelDataFileCopy(string file)
        {
            throw new NotImplementedException();
        }

        public void CancelDataFolderCopy(string folder)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string filename, string comment, ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level)
        {
            throw new NotImplementedException();
        }

        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level, bool regex)
        {
            throw new NotImplementedException();
        }

        public void ConslictsWith(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFile(string from, string to)
        {
            throw new NotImplementedException();
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            throw new NotImplementedException();
        }

        public void CopyPlugin(string from, string to)
        {
            throw new NotImplementedException();
        }

        public Form CreateCustomDialog()
        {
            throw new NotImplementedException();
        }

        public bool DataFileExists(string path)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string filename, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            throw new NotImplementedException();
        }

        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, bool regex)
        {
            throw new NotImplementedException();
        }

        public bool DialogYesNo(string msg) { return DialogYesNo(msg, "Question"); }

        public bool DialogYesNo(string msg, string title)
        {
            return MessageBox.Show(msg, title, MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void DisplayImage(string path) { DisplayImage(path, null); }

        public void DisplayImage(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path)
        {
            throw new NotImplementedException();
        }

        public void DisplayText(string path, string title)
        {
            throw new NotImplementedException();
        }

        public void DontInstallAnyDataFiles() { srd.InstallAllData = false; }

        public void DontInstallAnyPlugins() { srd.InstallAllPlugins = false; }

        public void DontInstallDataFile(string name)
        {
            CheckDataSafety(name);
            Program.strArrayRemove(srd.InstallData, name);
            if (!Program.strArrayContains(srd.IgnoreData, name)) srd.IgnoreData.Add(name);
        }

        public void DontInstallDataFolder(string folder, bool recurse)
        {
            CheckDataFolderSafety(folder);
            if (testMode)
            {
                folder = folder.ToLower();
                if (folder.EndsWith("\\") || folder.EndsWith("/")) folder = folder.Remove(folder.Length - 1);
                foreach (string s in dataFileList)
                {
                    if (s.StartsWith(folder) && (recurse || s.IndexOf('\\', folder.Length + 1) == -1))
                    {
                        Program.strArrayRemove(srd.InstallData, s);
                        if (!Program.strArrayContains(srd.IgnoreData, s)) srd.IgnoreData.Add(s);
                    }
                }
            }
            else
            {
                permissions.Assert();
                foreach (string path in Directory.GetFiles(DataFiles + folder, "*", recurse ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly))
                {
                    string file = Path.GetFullPath(path).Substring(DataFiles.Length);
                    Program.strArrayRemove(srd.InstallData, file);
                    if (!Program.strArrayContains(srd.IgnoreData, file)) srd.IgnoreData.Add(file);
                }
            }
        }

        public void DontInstallPlugin(string name)
        {
            CheckPluginSafety(name);
            Program.strArrayRemove(srd.InstallPlugins, name);
            if (!Program.strArrayContains(srd.IgnorePlugins, name)) srd.IgnorePlugins.Add(name);
        }

        public void EditINI(string section, string key, string value)
        {
            throw new NotImplementedException();
        }

        public void EditShader(byte package, string name, string path)
        {
            throw new NotImplementedException();
        }

        public void EditXMLLine(string file, int line, string value)
        {
            CheckDataSafety(file);
            string ext = Path.GetExtension(file).ToLower();
            if (ext != ".txt" && ext != ".xml" && ext != ".bat" && ext != ".ini") throw new Exception("Can only edit files with a .xml, .ini, .bat or .txt extension");
            permissions.Assert();
            string[] lines = File.ReadAllLines(DataFiles + file);
            if (line < 0 || line >= lines.Length) throw new Exception("Invalid line number");
            lines[line] = value;
            File.WriteAllLines(DataFiles + file, lines);

        }

        public void EditXMLReplace(string file, string find, string replace)
        {
            CheckDataSafety(file);
            string ext = Path.GetExtension(file).ToLower();
            if (ext != ".txt" && ext != ".xml" && ext != ".bat" && ext != ".ini") throw new Exception("Can only edit files with a .xml, .ini, .bat or .txt extension");
            permissions.Assert();
            string text = File.ReadAllText(DataFiles + file);
            text = text.Replace(find, replace);
            File.WriteAllText(DataFiles + file, text);
        }

        public void FatalError() { srd.CancelInstall = true; }

        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel)
        {
            throw new NotImplementedException();
        }

        public void GenerateNewDataFile(string file, byte[] data)
        {
            throw new NotImplementedException();
        }

        public string[] GetActiveEspNames()
        {
            permissions.Assert();
            List<string> names = new List<string>();
            List<EspInfo> Esps = Program.Data.Esps;
            for (int i = 0; i < Esps.Count; i++) if (Esps[i].Active) names.Add(Program.Data.Esps[i].FileName);
            return names.ToArray();
        }

        public string[] GetActiveOmodNames()
        {
            throw new NotImplementedException();
        }

        public byte[] GetDataFileFromBSA(string file)
        {
            CheckPathSafety(file);
            permissions.Assert();
            return Classes.BSAArchive.GetFileFromBSA(file);
        }

        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            CheckPathSafety(file);
            permissions.Assert();
            return Classes.BSAArchive.GetFileFromBSA(bsa, file);
        }

        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return testMode ? SimulateFSOutput(dataFileList, path, pattern, recurse)
                : StripPathList(GetFilePaths(DataFiles + path, pattern, recurse), DataFiles.Length);
        }

        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return testMode ? SimulateFSOutput(dataFolderList, path, pattern, recurse)
                : StripPathList(GetDirectoryPaths(DataFiles + path, pattern, recurse), DataFiles.Length);
        }

        public bool GetDisplayWarnings() { return false; }

        public string[] GetExistingEspNames()
        {
            permissions.Assert();
            string[] names = new string[Program.Data.Esps.Count];
            for (int i = 0; i < names.Length; i++) names[i] = Program.Data.Esps[i].FileName;
            return names;
        }

        // TODO: OBMM had to be placed inside the oblivion folder, need to change that
        public Version GetOBGEVersion()
        {
            permissions.Assert();
            if (!File.Exists(Path.Combine(Program.CurrentDir,"obse","plugins")+"\\obge.dll")) return null;
            else return new Version(FileVersionInfo.GetVersionInfo(Path.Combine(Program.CurrentDir, "obse", "plugins") + "\\obge.dll").FileVersion.Replace(", ", "."));
        }

        public Version GetOblivionVersion()
        {
            permissions.Assert();
            return new Version(FileVersionInfo.GetVersionInfo(Program.CurrentDir+"oblivion.exe").FileVersion.Replace(", ", "."));
        }

        public Version GetOBMMVersion()
        {
            return new Version(Program.MajorVersion, Program.MinorVersion, Program.BuildNumber, 0);
        }

        public Version GetOBSEPluginVersion(string plugin)
        {
            plugin = Path.ChangeExtension(Path.Combine(Program.CurrentDir, "obse", "plugins", plugin), ".dll");
            CheckPathSafety(plugin);
            permissions.Assert();
            if (!File.Exists(plugin)) return null;
            else return new Version(FileVersionInfo.GetVersionInfo(plugin).FileVersion.Replace(", ", "."));
        }

        public Version GetOBSEVersion()
        {
            permissions.Assert();
            if (!File.Exists("obse_loader.exe")) return null;
            else return new Version(FileVersionInfo.GetVersionInfo(Program.CurrentDir + "obse_loader.exe").FileVersion.Replace(", ", "."));
        }

        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return testMode ? SimulateFSOutput(pluginFolderList, path, pattern, recurse)
                : StripPathList(GetDirectoryPaths(Plugins + path, pattern, recurse), Plugins.Length);
        }

        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return testMode ? SimulateFSOutput(pluginList, path, pattern, recurse)
                : StripPathList(GetFilePaths(Plugins + path, pattern, recurse), Plugins.Length);
        }

        public string InputString() { return InputString("", ""); }

        public string InputString(string title) { return InputString(title, ""); }

        public string InputString(string title, string initial)
        {
            permissions.Assert();
            using(TextEditor te = new TextEditor(title, initial, false, true))
            {
                te.ShowDialog();
                if (te.DialogResult != DialogResult.Yes) return "";
                else return te.Result;
            }
        }

        public void InstallAllDataFiles() { srd.InstallAllData = true; }

        public void InstallAllPlugins() { srd.InstallAllPlugins = true; }

        public void InstallDataFile(string name)
        {
            CheckDataSafety(name);
            Program.strArrayRemove(srd.IgnoreData, name);
            if (!Program.strArrayContains(srd.InstallData, name)) srd.InstallData.Add(name);
        }

        public void InstallDataFolder(string folder, bool recurse)
        {
            CheckDataFolderSafety(folder);
            if (testMode)
            {
                folder = folder.ToLower();
                if (folder.EndsWith("\\") || folder.EndsWith("/")) folder = folder.Remove(folder.Length - 1);
                foreach (string s in dataFileList)
                {
                    if (s.StartsWith(folder) && (recurse || s.IndexOf('\\', folder.Length + 1) == -1))
                    {
                        Program.strArrayRemove(srd.IgnoreData, s);
                        if (!Program.strArrayContains(srd.InstallData, s)) srd.InstallData.Add(s);
                    }
                }
            }
            else
            {
                permissions.Assert();
                foreach (string path in Directory.GetFiles(DataFiles + folder, "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    string file = Path.GetFullPath(path).Substring(DataFiles.Length);
                    Program.strArrayRemove(srd.IgnoreData, file);
                    if (!Program.strArrayContains(srd.InstallData, file)) srd.InstallData.Add(file);
                }
            }
        }

        public void InstallPlugin(string name)
        {
            CheckPluginSafety(name);
            Program.strArrayRemove(srd.IgnorePlugins, name);
            if (!Program.strArrayContains(srd.InstallPlugins, name)) srd.InstallPlugins.Add(name);
        }

        public bool IsSimulation()
        {
            throw new NotImplementedException();
        }

        public void LoadAfter(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void LoadBefore(string plugin1, string plugin2)
        {
            throw new NotImplementedException();
        }

        public void LoadEarly(string plugin)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg)
        {
            throw new NotImplementedException();
        }

        public void Message(string msg, string title)
        {
            throw new NotImplementedException();
        }

        public void PatchDataFile(string from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public void PatchPlugin(string from, string to, bool create)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadDataFile(string file)
        {
            CheckDataSafety(file);
            permissions.Assert();
            return File.ReadAllBytes(Path.Combine(DataFiles, file));
        }

        public byte[] ReadExistingDataFile(string file)
        {
            CheckPathSafety(file);
            permissions.Assert();
            return File.ReadAllBytes("data\\" + file);
        }

        public string ReadINI(string section, string value)
        {
            permissions.Assert();
            return OblivionINI.GetINIValue(section, value);
        }

        public string ReadRendererInfo(string value)
        {
            throw new NotImplementedException();
        }

        public void RegisterBSA(string path)
        {
            throw new NotImplementedException();
        }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            throw new NotImplementedException();
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning)
        {
            throw new NotImplementedException();
        }

        public void SetGlobal(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetGMST(string file, string edid, string value)
        {
            throw new NotImplementedException();
        }

        public void SetNewLoadOrder(string[] plugins)
        {
            throw new NotImplementedException();
        }

        public void SetPluginByte(string file, long offset, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            throw new NotImplementedException();
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            throw new NotImplementedException();
        }

        public void UncheckEsp(string plugin)
        {
            throw new NotImplementedException();
        }

        public void UnregisterBSA(string path)
        {
            throw new NotImplementedException();
        }
    }
}
