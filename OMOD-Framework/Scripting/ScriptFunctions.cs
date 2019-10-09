using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using OMODFramework;
using OMODFramework.Scripting;

namespace OblivionModManager.Scripting
{
    internal class ScriptFunctions : IScriptFunctions
    {
        private readonly ScriptReturnData srd;
        private readonly string DataFiles;
        private readonly string Plugins;

        private static Framework f;

        private static Func<string, string, int> IDialogYesNo;
        private static Func<string, bool> ExistsFile;
        private static Func<string, Version> GetFileVersion;
        private static Func<string[], string, bool, string[], string[], int[]> DialogSelect;
        private static Action<string, string> IMessage;
        private static Action<string> IDisplayImage;
        private static Action<string, string, bool> IDisplayText;
        private static Func<string, string, string> IInputString;
        private static Func<string[]> IGetActiveESPNames;
        private static Func<string[]> IGetExistingESPNames;
        private static Func<string, string> IGetFile;

        internal ScriptFunctions(ScriptReturnData srd, string dataFilesPath, string pluginsPath,
            Framework _f,
            IScriptRunnerFunctions scriptRunnerFunctions)
        {
            f = _f;
            //Warn = scriptRunnerFunctions.Warn;
            IDialogYesNo = scriptRunnerFunctions.DialogYesNo;
            ExistsFile = scriptRunnerFunctions.ExistsFile;
            GetFileVersion = scriptRunnerFunctions.GetFileVersion;
            DialogSelect = scriptRunnerFunctions.DialogSelect;
            IMessage = scriptRunnerFunctions.Message;
            IDisplayImage = scriptRunnerFunctions.DisplayImage;
            IDisplayText = scriptRunnerFunctions.DisplayText;
            IInputString = scriptRunnerFunctions.InputString;
            IGetActiveESPNames = scriptRunnerFunctions.GetActiveESPNames;
            IGetFile = scriptRunnerFunctions.GetFileFromPath;
            IGetExistingESPNames = scriptRunnerFunctions.GetExistingESPNames;

            this.srd = srd;
            DataFiles = dataFilesPath;
            Plugins = pluginsPath;
        }

        private void CheckPathSafety(string path) { if (!Framework.IsSafeFileName(path)) throw new Exception("Illegal file name: " + path); }

        private void CheckPluginSafety(string path) { if (!Framework.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path); }

        private void CheckDataSafety(string path) { if (!Framework.IsSafeFileName(path)) throw new ScriptingException("Illegal file name: " + path); }

        private void CheckFolderSafety(string path) { if (!Framework.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path); }

        private void CheckPluginFolderSafety(string path)
        {
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Framework.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
        }

        private void CheckDataFolderSafety(string path)
        {
            if (path.EndsWith("\\") || path.EndsWith("/")) path = path.Remove(path.Length - 1);
            if (!Framework.IsSafeFolderName(path)) throw new ScriptingException("Illegal folder name: " + path);
        }

        private string[] GetFilePaths(string path, string pattern, bool recurse)
        {
            return Directory.GetFiles(path, !string.IsNullOrEmpty(pattern) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private string[] GetDirectoryPaths(string path, string pattern, bool recurse)
        {
            return Directory.GetDirectories(path, !string.IsNullOrEmpty(pattern) ? pattern : "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private string[] StripPathList(string[] paths, int baseLength)
        {
            for (var i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(baseLength);
            for (var i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(1);
            return paths;
        }

        #region Functions


        public void CancelDataFileCopy(string file)
        {
            CheckPathSafety(file);
            var tempFile = Path.Combine(DataFiles, file);
            var toL = file.ToLower();
            for (var i = 0; i < srd.CopyDataFiles.Count; i++)
            {
                if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
            }
            File.Delete(tempFile);
        }
        public void CancelDataFolderCopy(string folder)
        {
            CheckPathSafety(folder);
            var toL = folder.ToLower();
            for (var i = 0; i < srd.CopyDataFiles.Count; i++)
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
        public void ConflictsWith(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, ConflictLevel level, bool regex) { }
        public void CopyDataFile(string from, string to)
        {
            CheckDataSafety(from);
            CheckPathSafety(to);
            var toL = to.ToLower();
            if (toL.EndsWith(".esm") || toL.EndsWith(".esp")) throw new Exception("Esm and Esp files are illegal");
            for (var i = 0; i < srd.CopyDataFiles.Count; i++)
            {
                if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
            }
            srd.CopyDataFiles.Add(new ScriptCopyDataFile(from, to));
        }
        public void CopyDataFolder(string from, string to, bool recurse)
        {
            CheckDataFolderSafety(from);
            CheckFolderSafety(to);
            from = Path.GetFullPath(Path.Combine(DataFiles, from));
            foreach (var path in Directory.GetFiles(from, "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var fileFrom = Path.GetFullPath(path).Substring(DataFiles.Length);
                var fileTo = Path.GetFullPath(path).Substring(from.Length);
                if (fileTo.StartsWith("" + Path.DirectorySeparatorChar) || fileTo.StartsWith("" + Path.AltDirectorySeparatorChar)) fileTo = fileTo.Substring(1);
                fileTo = Path.Combine(to, fileTo);
                var toL = fileTo.ToLower();
                for (var i = 0; i < srd.CopyDataFiles.Count; i++)
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
            var toL = to.ToLower();
            if (!toL.EndsWith(".esp") && !toL.EndsWith(".esm")) throw new Exception("Copied plugins must have a .esp or .esm file extension");
            if (to.Contains("\\") || to.Contains("/")) throw new Exception("Cannot copy a plugin to a subdirectory of the data folder");
            for (var i = 0; i < srd.CopyPlugins.Count; i++)
            {
                if (srd.CopyPlugins[i].CopyTo == toL) srd.CopyPlugins.RemoveAt(i--);
            }
            srd.CopyPlugins.Add(new ScriptCopyDataFile(from, to));
        }
        public Form CreateCustomDialog() { return new Form(); }
        public bool DataFileExists(string path)
        {
            CheckPathSafety(path);
            return ExistsFile(Path.Combine("data", path));
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
        public void DependsOn(string name, int minMajorVersion, int minMinorVersion, int maxMajorVersion, int maxMinorVersion, string comment, bool regex) { }
        public bool DialogYesNo(string msg) { return DialogYesNo(msg, "Question"); }
        public bool DialogYesNo(string msg, string title) { return IDialogYesNo(msg, title) == 1; }
        public void DisplayImage(string path) { DisplayImage(path, null); }
        public void DisplayImage(string path, string title) { IDisplayImage(Path.Combine(DataFiles, path)); }
        public void DisplayText(string path) { DisplayText(path, null); }
        public void DisplayText(string path, string title)
        {
            CheckDataSafety(path);
            var s = File.ReadAllText(Path.Combine(DataFiles, path), Encoding.Default);
            IDisplayText(title ?? path, s, true);
        }
        public void DontInstallAnyDataFiles() { srd.InstallAllData = false; }
        public void DontInstallAnyPlugins() { srd.InstallAllPlugins = false; }
        public void DontInstallDataFile(string name)
        {
            CheckDataSafety(name);
            Framework.strArrayRemove(srd.InstallData, name);
            if (!Framework.strArrayContains(srd.IgnoreData, name)) srd.IgnoreData.Add(name);
        }
        public void DontInstallDataFolder(string folder, bool recurse)
        {
            CheckDataFolderSafety(folder);
            foreach (var path in Directory.GetFiles(Path.Combine(DataFiles, folder), "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var file = Path.GetFullPath(path).Substring(DataFiles.Length);
                Framework.strArrayRemove(srd.InstallData, file);
                if (!Framework.strArrayContains(srd.IgnoreData, file)) srd.IgnoreData.Add(file);
            }
        }
        public void DontInstallPlugin(string name)
        {
            CheckPluginSafety(name);
            Framework.strArrayRemove(srd.InstallPlugins, name);
            if (!Framework.strArrayContains(srd.IgnorePlugins, name)) srd.IgnorePlugins.Add(name);
        }
        public void EditINI(string section, string key, string value) { srd.INIEdits.Add(new INIEditInfo(section, key, value)); }
        public void EditShader(byte package, string name, string path)
        {
            CheckDataSafety(path);
            srd.SDPEdits.Add(new SDPEditInfo(package, name, DataFiles + path));
        }
        public void EditXMLLine(string file, int line, string value)
        {
            CheckDataSafety(file);
            var ext = Path.GetExtension(file)?.ToLower();
            if (ext != ".txt" && ext != ".xml" && ext != ".bat" && ext != ".ini") throw new Exception("Can only edit files with a .xml, .ini, .bat or .txt extension");
            string[] lines = File.ReadAllLines(Path.Combine(DataFiles + file));
            if (line < 0 || line >= lines.Length) throw new Exception("Invalid line number");
            lines[line] = value;
            File.WriteAllLines(Path.Combine(DataFiles + file), lines);

        }
        public void EditXMLReplace(string file, string find, string replace)
        {
            CheckDataSafety(file);
            var ext = Path.GetExtension(file)?.ToLower();
            if (ext != ".txt" && ext != ".xml" && ext != ".bat" && ext != ".ini") throw new Exception("Can only edit files with a .xml, .ini, .bat or .txt extension");
            var text = File.ReadAllText(Path.Combine(DataFiles + file));
            text = text.Replace(find, replace);
            File.WriteAllText(Path.Combine(DataFiles + file), text);
        }
        public void FatalError() { srd.CancelInstall = true; }
        public void GenerateBSA(string file, string path, string prefix, int cRatio, int cLevel) { }
        public void GenerateNewDataFile(string file, byte[] data)
        {
            CheckPathSafety(file);
            var tempFile = Path.Combine(DataFiles, file);
            if (!File.Exists(tempFile))
            {
                var toL = file.ToLower();
                if (toL.EndsWith(".esm") || toL.EndsWith(".esp")) throw new Exception("Data files can't be an esp or esm");
                for (var i = 0; i < srd.CopyDataFiles.Count; i++)
                {
                    if (srd.CopyDataFiles[i].CopyTo == toL) srd.CopyDataFiles.RemoveAt(i--);
                }
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(file, tempFile));
            }
            if (!Directory.Exists(Path.GetDirectoryName(tempFile))) Directory.CreateDirectory(Path.GetDirectoryName(tempFile));
            File.WriteAllBytes(tempFile, data);
        }
        public string[] GetActiveEspNames() { return IGetActiveESPNames(); }
        public string[] GetActiveOmodNames() { return new[] { "" }; }
        public byte[] GetDataFileFromBSA(string file)
        {
            //CheckPathSafety(file);
            //return BSAArchive.GetFileFromBSA(file);
            //throw new NotImplementedException();
            return new byte[] { 0 };
        }
        public byte[] GetDataFileFromBSA(string bsa, string file)
        {
            //CheckPathSafety(file);
            //return BSAArchive.GetFileFromBSA(bsa, file);
            //throw new NotImplementedException();
            return new byte[] { 0 };
        }
        public string[] GetDataFiles(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return StripPathList(GetFilePaths(Path.Combine(DataFiles, path), pattern, recurse), DataFiles.Length);
        }
        public string[] GetDataFolders(string path, string pattern, bool recurse)
        {
            CheckDataFolderSafety(path);
            return StripPathList(GetDirectoryPaths(Path.Combine(DataFiles, path), pattern, recurse), DataFiles.Length);
        }
        public bool GetDisplayWarnings() { return false; }
        public string[] GetExistingEspNames() { return IGetExistingESPNames(); }
        public Version GetOBGEVersion() { return GetFileVersion(Path.Combine("data", "obse", "plugins", "obge.dll")); }
        public Version GetOblivionVersion() { return GetFileVersion("oblivion.exe"); }
        public Version GetOBMMVersion() { return new Version(f.OBMMFakeMajorVersion, f.OBMMFakeMinorVersion, f.OBMMFakeBuildNumber, 0); }
        public Version GetOBSEPluginVersion(string plugin)
        {
            plugin = Path.ChangeExtension(Path.Combine("data", "obse", "plugins", plugin), ".dll");
            CheckPathSafety(plugin);
            if (!File.Exists(plugin)) return null;
            return GetFileVersion(plugin);
        }
        public Version GetOBSEVersion() { return GetFileVersion("obse_loader.exe"); }
        public string[] GetPluginFolders(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return StripPathList(GetDirectoryPaths(Path.Combine(Plugins, path), pattern, recurse), Plugins.Length);
        }
        public string[] GetPlugins(string path, string pattern, bool recurse)
        {
            CheckPluginFolderSafety(path);
            return StripPathList(GetFilePaths(Path.Combine(Plugins, path), pattern, recurse), Plugins.Length);
        }
        public string InputString() { return InputString("", ""); }
        public string InputString(string title) { return InputString(title, ""); }
        public string InputString(string title, string initial)
        {
            var result = IInputString(title, initial);
            return result;
        }
        public void InstallAllDataFiles() { srd.InstallAllData = true; }
        public void InstallAllPlugins() { srd.InstallAllPlugins = true; }
        public void InstallDataFile(string name)
        {
            CheckDataSafety(name);
            Framework.strArrayRemove(srd.IgnoreData, name);
            if (!Framework.strArrayContains(srd.InstallData, name)) srd.InstallData.Add(name);
        }
        public void InstallDataFolder(string folder, bool recurse)
        {
            CheckDataFolderSafety(folder);
            foreach (var path in Directory.GetFiles(Path.Combine(DataFiles, folder), "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var file = Path.GetFullPath(path).Substring(DataFiles.Length);
                Framework.strArrayRemove(srd.IgnoreData, file);
                if (!Framework.strArrayContains(srd.InstallData, file)) srd.InstallData.Add(file);
            }
        }
        public void InstallPlugin(string name)
        {
            CheckPluginSafety(name);
            Framework.strArrayRemove(srd.IgnorePlugins, name);
            if (!Framework.strArrayContains(srd.InstallPlugins, name)) srd.InstallPlugins.Add(name);
        }
        public bool IsSimulation() { return false; }
        public void LoadAfter(string plugin1, string plugin2) { LoadOrder(plugin1, plugin2, true, false); }
        public void LoadBefore(string plugin1, string plugin2) { LoadOrder(plugin1, plugin2, false, false); }
        public void LoadEarly(string plugin)  { LoadOrder(plugin, null, false, true); }
        private void LoadOrder(string plugin1, string plugin2, bool after, bool early)
        {
            CheckPathSafety(plugin1);
            var path1 = plugin1.ToLower();
            var found = false;
            for (var i = 0; i < srd.CopyPlugins.Count; i++)
            {
                if (srd.CopyPlugins[i].CopyTo == path1)
                {
                    found = true;
                    break;
                }
            }
            if (!found) CheckPluginSafety(plugin1);
            if (early)
            {
                srd.LoadOrderList.Add(new PluginLoadInfo(plugin1, null, false, true));
            }
            else
            {
                CheckPathSafety(plugin2);
                plugin1 = plugin1.ToLower();
                plugin2 = plugin2.ToLower();
                for (var i = 0; i < srd.LoadOrderList.Count; i++)
                {
                    if (plugin1 == srd.LoadOrderList[i].Plugin && plugin2 == srd.LoadOrderList[i].Target) srd.LoadOrderList.RemoveAt(i--);
                }
                srd.LoadOrderList.Add(new PluginLoadInfo(plugin1, plugin2, after, false));
            }
        }

        public void Message(string msg) { IMessage(msg, null); }
        public void Message(string msg, string title) { IMessage(msg, title); }
        public void PatchDataFile(string from, string to, bool create)
        {/*
            CheckDataSafety(from);
            CheckPathSafety(to);
            string toL = to.ToLower();
            if (toL.EndsWith(".esp") || toL.EndsWith(".esm")) throw new Exception("Cant be esp or esm files");
            to = IGetFile(to);

            //if (File.Exists(to))
            //{
            //    File.Delete(to);
            //}
            if (!File.Exists(to) && !create) return;
            File.Copy(Path.Combine(DataFiles, from), Path.Combine(Framework.OutputDir, to));*/
        }
        public void PatchPlugin(string from, string to, bool create)
        {/*
            CheckDataSafety(from);
            CheckPathSafety(to);
            string toL = to.ToLower();
            if (!toL.EndsWith(".esp") && !toL.EndsWith(".esm")) throw new Exception("Must be esp or esm files");
            to = IGetFile(to);

            //if (File.Exists(to))
            //{
            //File.Delete(to);
            //}
            //else if (!create) return;
            if (!File.Exists(to) && !create) return;
            File.Copy(Path.Combine(Plugins, from), Path.Combine(Framework.OutputDir, to));*/
        }
        public byte[] ReadDataFile(string file)
        {
            CheckDataSafety(file);
            return File.ReadAllBytes(Path.Combine(DataFiles, file));
        }
        public byte[] ReadExistingDataFile(string file)
        {
            CheckPathSafety(file);
            return File.ReadAllBytes(IGetFile(file));
        }
        public string ReadINI(string section, string value) { return ""; }
        public string ReadRendererInfo(string value) { return ""; }
        public void RegisterBSA(string path) { }
        public string[] Select(string[] items, string[] previews, string[] descs, string title, bool many)
        {
            if (previews != null)
            {
                for (var i = 0; i < previews.Length; i++)
                {
                    if (previews[i] != null)
                    {
                        CheckDataSafety(previews[i]);
                        previews[i] = DataFiles + previews[i];
                    }
                }
            }
            int[] r = DialogSelect(items, title, many, previews, descs);
            var result = new string[r.Length];
            for (var i = 0; i < r.Length; i++)
            {
                result[i] = items[r[i]];
            }
            return result;
        }
        public void SetDeactivationWarning(string plugin, DeactiveStatus warning) { }
        public void SetGlobal(string file, string edid, string value) { }
        public void SetGMST(string file, string edid, string value) { }
        public void SetNewLoadOrder(string[] plugins) { }
        public void SetPluginByte(string file, long offset, byte value)
        {
            CheckPluginSafety(file);
            using (var fs = File.OpenWrite(Path.Combine(Plugins, file)))
            {
                fs.Position = offset;
                fs.WriteByte(value);
            }
        }
        public void SetPluginFloat(string file, long offset, float value)
        {
            CheckPluginSafety(file);
            byte[] data = BitConverter.GetBytes(value);
            using (var fs = File.OpenWrite(Path.Combine(Plugins, file)))
            {
                fs.Position = offset;
                fs.Write(data, 0, 2);
            }
        }
        public void SetPluginInt(string file, long offset, int value)
        {
            CheckPluginSafety(file);
            byte[] data = BitConverter.GetBytes(value);
            using (var fs = File.OpenWrite(Path.Combine(Plugins, file)))
            {
                fs.Position = offset;
                fs.Write(data, 0, 4);
            }
        }
        public void SetPluginLong(string file, long offset, long value)
        {
            CheckPluginSafety(file);
            byte[] data = BitConverter.GetBytes(value);
            using (var fs = File.OpenWrite(Path.Combine(Plugins, file)))
            {
                fs.Position = offset;
                fs.Write(data, 0, 8);
            }
        }
        public void SetPluginShort(string file, long offset, short value)
        {
            CheckPluginSafety(file);
            byte[] data = BitConverter.GetBytes(value);
            using (var fs = File.OpenWrite(Path.Combine(Plugins, file)))
            {
                fs.Position = offset;
                fs.Write(data, 0, 4);
            }
        }
        public void UncheckEsp(string plugin) { }
        public void UnregisterBSA(string path) { }

        #endregion
    }
}
