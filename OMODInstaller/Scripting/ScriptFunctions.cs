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
            CheckPathSafety(file);
            permissions.Assert();
            string tempFile = Path.Combine(DataFiles, file);
            string toL = file.ToLower();
            for(int i = 0; i < srd.CopyDataFiles.Count; i++)
            {
                if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
            }
            File.Delete(tempFile);
        }

        public void CancelDataFolderCopy(string folder)
        {
            CheckPathSafety(folder);
            permissions.Assert();
            string toL = folder.ToLower();
            for (int i = 0; i < srd.CopyDataFiles.Count; i++)
            {
                if (srd.CopyDataFiles[i].CopyTo.StartsWith(toL))
                {
                    File.Delete(Path.Combine(DataFiles, srd.CopyDataFiles[i].CopyTo));
                    srd.CopyDataFiles.RemoveAt(i--);
                }
            }
        }

        public void ConflictsWith(string filename) { ConflictsWith(filename, 0, 0, 0, 0, null, ConflictLevel.MajorConflict, false); }
        public void ConslictsWith(string filename, string comment) { ConflictsWith(filename, 0, 0, 0, 0, comment, ConflictLevel.MajorConflict, false); }
        public void ConflictsWith(string filename, string comment, ConflictLevel level) { ConflictsWith(filename, 0, 0, 0, 0, comment, level, false); }
        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null, ConflictLevel.MajorConflict, false);
        }
        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, ConflictLevel.MajorConflict, false);
        }
        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level)
        {
            ConflictsWith(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, level, false);
        }
        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level, bool regex)
        {
            ConflictData cd = new ConflictData
            {
                File = name,
                Comment = comment,
                level = level,
                MinMajorVersion = minMajorVersion,
                MinMinorVersion = minMinorVersion,
                MaxMajorVersion = maxMajorVersion,
                MaxMinorVersion = maxMinorVersion,
                Partial = regex
            };
            srd.ConflictsWith.Add(cd);
        }

        public void CopyDataFile(string from, string to)
        {
            CheckDataSafety(from);
            CheckPathSafety(to);
            string toL = to.ToLower();
            if (toL.EndsWith(".esm") || toL.EndsWith(".esp")) throw new Exception("Esm and Esp files are illegal");
            for(int i = 0; i < srd.CopyDataFiles.Count; i++)
            {
                if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
            }
            srd.CopyDataFiles.Add(new ScriptCopyDataFile(from, to));
        }

        public void CopyDataFolder(string from, string to, bool recurse)
        {
            CheckDataFolderSafety(from);
            CheckFolderSafety(to);

            permissions.Assert();
            from = Path.GetFullPath(Path.Combine(DataFiles, from));
            foreach(string path in Directory.GetFiles(from, "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                string fileFrom = Path.GetFullPath(path).Substring(DataFiles.Length);
                string fileTo = Path.GetFullPath(path).Substring(from.Length);
                if (fileTo.StartsWith("" + Path.DirectorySeparatorChar) || fileTo.StartsWith("" + Path.AltDirectorySeparatorChar)) fileTo = fileTo.Substring(1);
                fileTo = Path.Combine(to, fileTo);
                string toL = fileTo.ToLower();
                for(int i = 0; i < srd.CopyDataFiles.Count; i++)
                {
                    if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
                }
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(fileFrom, fileTo));
            }
        }

        public void CopyPlugin(string from, string to)
        {
            CheckPluginSafety(from);
            CheckPathSafety(to);
            string toL = to.ToLower();
            if (!toL.EndsWith(".esp") && !toL.EndsWith(".esm")) throw new Exception("Copied plugins must have a .esp or .esm file extension");
            if (to.Contains("\\") || to.Contains("/")) throw new Exception("Cannot copy a plugin to a subdirectory of the data folder");
            for (int i = 0; i < srd.CopyPlugins.Count; i++)
            {
                if (srd.CopyPlugins[i].CopyTo == toL) srd.CopyPlugins.RemoveAt(i--);
            }
            srd.CopyPlugins.Add(new ScriptCopyDataFile(from, to));
        }

        public Form CreateCustomDialog()
        {
            permissions.Assert();
            return new Form();
        }

        public bool DataFileExists(string path)
        {
            CheckPathSafety(path);
            permissions.Assert();
            return File.Exists(Program.DataDir + path);
        }

        public void DependsOn(string filename) { DependsOn(filename, 0, 0, 0, 0, null, false); }
        public void DependsOn(string filename, string comment) { DependsOn(filename, 0, 0, 0, 0, comment, false); }
        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, null, false);
        }
        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment)
        {
            DependsOn(name, minMajorVersion, minMinorVersion, maxMajorVersion, maxMinorVersion, comment, false);
        }
        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, bool regex)
        {
            ConflictData cd = new ConflictData();
            cd.File = name;
            cd.Comment = comment;
            cd.MinMajorVersion = minMajorVersion;
            cd.MinMinorVersion = minMinorVersion;
            cd.MaxMajorVersion = maxMajorVersion;
            cd.MaxMinorVersion = maxMinorVersion;
            cd.Partial = regex;
            srd.DependsOn.Add(cd);
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

        public void DisplayText(string path) { DisplayText(path, null); }

        public void DisplayText(string path, string title)
        {
            CheckDataSafety(path);
            permissions.Assert();
            string s = File.ReadAllText(DataFiles + path, System.Text.Encoding.Default);
            new TextEditor((title != null) ? title : path, s, true, false).ShowDialog();
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
            srd.INIEdits.Add(new INIEditInfo(section, key, value));
        }

        public void EditShader(byte package, string name, string path)
        {
            CheckDataSafety(path);
            srd.SDPEdits.Add(new SDPEditInfo(package, name, DataFiles + path));
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

        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel) { }

        public void GenerateNewDataFile(string file, byte[] data)
        {
            CheckPathSafety(file);
            permissions.Assert();
            string tempFile = Path.Combine(DataFiles, file);
            if (!File.Exists(tempFile))
            {
                string toL = file.ToLower();
                if (toL.EndsWith(".esm") || toL.EndsWith(".esp")) throw new Exception("Data files can't be an esp or esm");
                for(int i = 0; i < srd.CopyDataFiles.Count; i++)
                {
                    if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
                }
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(file, tempFile));
            }
            if(!Directory.Exists(Path.GetDirectoryName(tempFile))) Directory.CreateDirectory(Path.GetDirectoryName(tempFile));
            File.WriteAllBytes(tempFile, data);
        }

        public string[] GetActiveEspNames()
        {
            permissions.Assert();
            List<string> names = new List<string>();
            List<EspInfo> Esps = Program.Data.Esps;
            for (int i = 0; i < Esps.Count; i++) if (Esps[i].Active) names.Add(Program.Data.Esps[i].FileName);
            return names.ToArray();
        }

        /// <summary>
        /// This will always return an empty array due to this being an installer and not a omod manager
        /// </summary>
        /// <returns></returns>
        public string[] GetActiveOmodNames()
        {
            string[] names = new string[Program.Data.omods.Count];
            for (int i = 0; i < names.Length; i++) names[i] = Program.Data.omods[i].ModName;
            return names;
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

        public bool IsSimulation() { return false; }

        private enum LoadOrderTypes {AFTER, BEFORE, EARLY};

        public void LoadAfter(string plugin1, string plugin2) { CreateLoadOrderAdvise(plugin1, plugin2, LoadOrderTypes.AFTER); }

        public void LoadBefore(string plugin1, string plugin2)  { CreateLoadOrderAdvise(plugin1, plugin2, LoadOrderTypes.AFTER); }

        public void LoadEarly(string plugin) { CreateLoadOrderAdvise(plugin); }

        private void CreateLoadOrderAdvise(string plugin) { CreateLoadOrderAdvise(plugin, null, LoadOrderTypes.EARLY); }

        private void CreateLoadOrderAdvise(string plugin1, string plugin2, LoadOrderTypes type)
        {
            string adviseFile = Program.OutputDir + "loadorder_advise.txt";
            List<string> contents = new List<string>();
            if (File.Exists(adviseFile))
            {
                using(StreamReader sr = new StreamReader(File.OpenRead(adviseFile), System.Text.Encoding.Default))
                {
                    while(sr.Peek() != -1)
                    {
                        contents.Add(sr.ReadLine());
                    }
                }
                File.Delete(adviseFile);
            }
            switch (type)
            {
                case LoadOrderTypes.AFTER:
                    contents.Add($"Place {plugin1} after {plugin2}");
                    break;
                case LoadOrderTypes.BEFORE:
                    contents.Add($"Place {plugin1} before {plugin2}");
                    break;
                case LoadOrderTypes.EARLY:
                    contents.Add($"Place {plugin1} early in your load order");
                    break;
            }
            using(StreamWriter sw = new StreamWriter(File.Create(adviseFile), System.Text.Encoding.Default))
            {
                foreach(string s in contents)
                {
                    sw.WriteLine(s);
                }
            }
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

        public string ReadRendererInfo(string value) { return ""; }

        public void RegisterBSA(string path) { }

        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            throw new NotImplementedException();
        }

        public void SetDeactivationWarning(string plugin, DeactiveStatus warning) { }

        public void SetGlobal(string file, string edid, string value) { }

        public void SetGMST(string file, string edid, string value) { }

        public void SetNewLoadOrder(string[] plugins) {}

        public void SetPluginByte(string file, long offset, byte value)
        {
            CheckPluginSafety(file);
            permissions.Assert();
            using(FileStream fs = File.OpenWrite(Plugins+ file))
            {
                fs.Position = offset;
                fs.WriteByte(value);
            }
        }

        public void SetPluginFloat(string file, long offset, float value)
        {
            CheckPluginSafety(file);
            permissions.Assert();
            byte[] data = BitConverter.GetBytes(value);
            using (FileStream fs = File.OpenWrite(Plugins + file))
            {
                fs.Position = offset;
                fs.Write(data, 0, 2);
            }
        }

        public void SetPluginInt(string file, long offset, int value)
        {
            CheckPluginSafety(file);
            permissions.Assert();
            byte[] data = BitConverter.GetBytes(value);
            using (FileStream fs = File.OpenWrite(Plugins + file))
            {
                fs.Position = offset;
                fs.Write(data, 0, 4);
            }
        }

        public void SetPluginLong(string file, long offset, long value)
        {
            CheckPluginSafety(file);
            permissions.Assert();
            byte[] data = BitConverter.GetBytes(value);
            using (FileStream fs = File.OpenWrite(Plugins + file))
            {
                fs.Position = offset;
                fs.Write(data, 0, 8);
            }
        }

        public void SetPluginShort(string file, long offset, short value)
        {
            CheckPluginSafety(file);
            permissions.Assert();
            byte[] data = BitConverter.GetBytes(value);
            using (FileStream fs = File.OpenWrite(Plugins + file))
            {
                fs.Position = offset;
                fs.Write(data, 0, 4);
            }
        }

        public void UncheckEsp(string plugin) {}

        public void UnregisterBSA(string path) {}
    }
}
