using System;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;

namespace OblivionModManager
{
    [Serializable]
    internal class OMOD
    {
        protected class PrivateData
        {
            internal ZipFile modFile = null;
        }

        [NonSerialized]
        private PrivateData pD = new PrivateData();
        internal void RecreatePrivateData() { if (pD == null) pD = new PrivateData(); }

        internal readonly string FilePath;
        internal readonly string FileName;
        internal readonly string LowerFileName;
        internal readonly string ModName;
        internal readonly int MajorVersion;
        internal readonly int MinorVersion;
        internal readonly int BuildVersion;
        internal readonly string Description;
        internal readonly string Email;
        internal readonly string Website;
        internal readonly string Author;
        internal readonly DateTime CreationTime;
        internal readonly string[] AllPlugins;
        internal readonly DataFileInfo[] AllDataFiles;
        internal readonly CompressionType CompType;
        private readonly byte FileVersion;
        internal bool Hidden { get; } = false;

        internal string Version
        {
            get { return "" + MajorVersion + ((MinorVersion != -1) ? ("." + MinorVersion + ((BuildVersion != -1) ? ("." + BuildVersion) : "")) : ""); }
        }

        internal string FullFilePath
        {
            get { return FilePath + FileName; }
        }

        internal string[] Plugins;
        internal DataFileInfo[] DataFiles;
        internal string[] BSAs;
        internal List<INIEditInfo> INIEdits;
        internal List<SDPEditInfo> SDPEdits;

        internal ConflictLevel Conflict = ConflictLevel.NoConflict;
        internal readonly List<ConflictData> ConflictsWith = new List<ConflictData>();
        internal readonly List<ConflictData> DependsOn = new List<ConflictData>();

        private ZipFile ModFile
        {
            get
            {
                if (pD.modFile != null) return pD.modFile;
                pD.modFile = new ZipFile(FullFilePath);
                return pD.modFile;
            }
        }

        internal OMOD(string path)
        {
            FilePath = Path.GetDirectoryName(path) + "\\";
            FileName = Path.GetFileName(path);
            LowerFileName = FileName.ToLower();

            Stream Config = ExtractWholeFile("config");
            if (Config == null) throw new Exception("Could not find the config");

            using(BinaryReader br = new BinaryReader(Config))
            {
                byte version = br.ReadByte();
                FileVersion = version;
                if (version > Program.CurrentOmodVersion)
                {
                    throw new Exception("");
                }

                ModName = br.ReadString();
                MajorVersion = br.ReadInt32();
                MinorVersion = br.ReadInt32();
                Author = br.ReadString();
                Email = br.ReadString();
                Website = br.ReadString();
                Description = br.ReadString();
                if (version >= 2)
                {
                    CreationTime = DateTime.FromBinary(br.ReadInt64());
                }
                else
                {
                    string sCreationTime = br.ReadString();
                    if (!DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out CreationTime))
                    {
                        CreationTime = new DateTime(2006, 1, 1);
                    }
                }
                if (Description == "") Description = "No description";
                CompType = (CompressionType)br.ReadByte();
                if (version >= 1)
                {
                    BuildVersion = br.ReadInt32();
                }
                else BuildVersion = -1;

                AllPlugins = GetPluginList();
                AllDataFiles = GetDataList();

                Close();
            }
        }

        internal void Close()
        {
            if (pD.modFile != null)
            {
                pD.modFile.Close();
                pD.modFile = null;
            }
        }

        private void CreateDirectoryStructure()
        {
            for (int x = 0; x < DataFiles.Length; x++)
            {
                string s = Path.GetDirectoryName(DataFiles[x].FileName);
                if (Program.UseOutputDir)
                {
                    if (!Directory.Exists(Program.OutputDir + s)) Directory.CreateDirectory(Program.OutputDir + s);
                }
                else
                {
                    if (!Directory.Exists(Program.CurrentDir + s)) Directory.CreateDirectory(Program.CurrentDir + s);
                }
            }
        }

        private ScriptExecutationData ExecuteScript(string plugins, string data)
        {
            ScriptReturnData srd = Scripting.ScriptRunner.ExecuteScript(GetScript(), data, plugins);

            if (srd.CancelInstall) return null;

            // create conflict advise file
            if(srd.ConflictsWith.Count > 0)
            {
                string conflictAdvise = Program.OutputDir + "conflict_advise.txt";
                List<string> conflictContents = new List<string>();
                if (File.Exists(conflictAdvise))
                {
                    using (StreamReader sr = new StreamReader(File.OpenRead(conflictAdvise), System.Text.Encoding.Default))
                    {
                        while (sr.Peek() != -1)
                        {
                            conflictContents.Add(sr.ReadLine());
                        }
                    }
                }
                File.Delete(conflictAdvise);
                foreach (ConflictData cd in srd.ConflictsWith)
                {
                    conflictContents.Add($"This mod conflicts with {cd.File}, " +
                            $"Max Version: {cd.MaxMajorVersion}.{cd.MaxMajorVersion}, " +
                            $"Min Version: {cd.MinMajorVersion}.{cd.MinMinorVersion}, " +
                            $"Conflict level: {cd.level}, " +
                            $"Comment: {cd.Comment}");
                }
                using (StreamWriter sw = new StreamWriter(File.Create(conflictAdvise), System.Text.Encoding.Default))
                {
                    foreach (string s in conflictContents)
                    {
                        sw.WriteLine(s);
                    }
                }
            }
            if(srd.DependsOn.Count > 0)
            {
                // create depends advise file
                string dependsAdvise = Program.OutputDir + "depends_advise.txt";
                List<string> dependsContents = new List<string>();
                if (File.Exists(dependsAdvise))
                {
                    using (StreamReader sr = new StreamReader(File.OpenRead(dependsAdvise), System.Text.Encoding.Default))
                    {
                        while (sr.Peek() != -1)
                        {
                            dependsContents.Add(sr.ReadLine());
                        }
                    }
                }
                File.Delete(dependsAdvise);
                foreach (ConflictData cd in srd.DependsOn)
                {
                    dependsContents.Add($"This mod depends on {cd.File}, " +
                            $"Max Version: {cd.MaxMajorVersion}.{cd.MaxMajorVersion}, " +
                            $"Min Version: {cd.MinMajorVersion}.{cd.MinMinorVersion}, " +
                            $"Comment: {cd.Comment}");
                }
                using (StreamWriter sw = new StreamWriter(File.Create(dependsAdvise), System.Text.Encoding.Default))
                {
                    foreach (string s in dependsContents)
                    {
                        sw.WriteLine(s);
                    }
                }
            }

            List<string> strtemp1 = new List<string>();

            // put all data files to be installed in the data file array
            if (srd.InstallAllPlugins)
            {
                foreach (string s in AllPlugins) if (!s.Contains("\\")) strtemp1.Add(s);
            }
            foreach (string s in srd.InstallPlugins) { if (!Program.strArrayContains(strtemp1, s)) strtemp1.Add(s); }
            foreach (string s in srd.IgnorePlugins) { Program.strArrayRemove(strtemp1, s); }
            foreach (ScriptCopyDataFile scd in srd.CopyPlugins)
            {
                if (!File.Exists(plugins + scd.CopyFrom))
                {
                    MessageBox.Show($"The script attempted to copy the plugin {scd.hCopyFrom} but it did not exist", "Warning");
                }
                else
                {
                    if (scd.CopyFrom != scd.CopyTo)
                    {
                        if (File.Exists(plugins + scd.CopyTo)) File.Delete(plugins + scd.CopyTo);
                        File.Copy(plugins + scd.CopyFrom, plugins + scd.hCopyTo);
                    }
                    if (!Program.strArrayContains(strtemp1, scd.CopyTo)) strtemp1.Add(scd.hCopyTo);
                }
            }
            for (int i = 0; i < strtemp1.Count; i++) if (!File.Exists(plugins + strtemp1[i])) strtemp1.RemoveAt(i--);
            Plugins = strtemp1.ToArray();
            strtemp1.Clear();
            // put all data files to be installed in the data file array
            if (srd.InstallAllData)
            {
                for (int i = 0; i < AllDataFiles.Length; i++) strtemp1.Add(AllDataFiles[i].FileName);
            }
            foreach (string s in srd.InstallData) { if (!Program.strArrayContains(strtemp1, s)) strtemp1.Add(s); }
            foreach (string s in srd.IgnoreData) { Program.strArrayRemove(strtemp1, s); }
            foreach (ScriptCopyDataFile scd in srd.CopyDataFiles)
            {
                if (!File.Exists(data + scd.CopyFrom))
                {
                    MessageBox.Show($"The script attempted to copy the data file {scd.hCopyFrom} but it did not exist", "Warning");
                }
                else
                {
                    if (scd.CopyFrom != scd.CopyTo)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(data + scd.CopyTo))) Directory.CreateDirectory(Path.GetDirectoryName(data + scd.hCopyTo));
                        if (File.Exists(data + scd.CopyTo)) File.Delete(data + scd.CopyTo);
                        File.Copy(data + scd.CopyFrom, data + scd.hCopyTo);
                    }
                    if (!Program.strArrayContains(strtemp1, scd.CopyTo)) strtemp1.Add(scd.hCopyTo);
                }
            }
            for (int i = 0; i < strtemp1.Count; i++) if (!File.Exists(data + strtemp1[i])) strtemp1.RemoveAt(i--);
            List<DataFileInfo> dtemp1 = new List<DataFileInfo>();
            foreach (string s in strtemp1)
            {
                DataFileInfo dfi;//=Program.Data.GetDataFile(s);
                dfi = Program.strArrayGet(AllDataFiles, s);
                if (dfi != null) dtemp1.Add(new DataFileInfo(dfi));
                //else dtemp1.Add(new DataFileInfo(s, CompressionHandler.CRC(data + s)));
            }

            DataFiles = dtemp1.ToArray();
            strtemp1.Clear();
            dtemp1.Clear();

            //TODO: Register BSAs
            INIEdits = new List<INIEditInfo>();
            foreach (INIEditInfo iei in srd.INIEdits)
            {
                iei.OldValue = OblivionINI.GetINIValue(iei.Section, iei.Name);
                iei.Plugin = this;
                OblivionINI.WriteINIValue(iei.Section, iei.Name, iei.NewValue);
                Program.Data.INIEdits.Add(iei);
                INIEdits.Add(iei);
            }
            if (INIEdits.Count == 0) INIEdits = null;

            SDPEdits = new List<SDPEditInfo>();
            foreach(SDPEditInfo sei in srd.SDPEdits)
            {
                if (Classes.OblivionSDP.EditShader(sei.Package, sei.Shader, sei.BinaryObject, FileName)) SDPEdits.Add(sei);
                sei.BinaryObject = null;
            }
            if (SDPEdits.Count == 0) SDPEdits = null;

            //return
            ScriptExecutationData sed = new ScriptExecutationData
            {
                //sed.PluginOrder = srd.LoadOrderList.ToArray();
                UncheckedPlugins = srd.UncheckedPlugins.ToArray(),
                EspDeactivationWarning = srd.EspDeactivation.ToArray()
                //sed.EspEdits = srd.EspEdits.ToArray();
                //sed.EarlyPlugins = srd.EarlyPlugins.ToArray();
            };
            return sed;
        }

        public void InstallOMOD()
        {
            //Extract plugins and data files
            string PluginsPath = GetPlugins();
            string DataPath = GetDataFiles();
            if (PluginsPath != null) PluginsPath = Path.GetFullPath(PluginsPath);
            if (DataPath != null) DataPath = Path.GetFullPath(DataPath);
            //Run the script
            ScriptExecutationData sed = ExecuteScript(PluginsPath, DataPath);
            if (sed == null) return;

            //copy data files
            CreateDirectoryStructure();
            for(int i = 0; i < DataFiles.Length; i++)
            {
                File.Move(DataPath + DataFiles[i].FileName, Program.OutputDir + DataFiles[i].FileName);
            }
        }

        private string[] GetPluginList()
        {
            Stream TempStream = ExtractWholeFile("plugins.crc");
            if (TempStream == null) return new string[0];
            BinaryReader br = new BinaryReader(TempStream);
            List<string> ar = new List<string>();
            while (br.PeekChar() != -1)
            {
                ar.Add(br.ReadString());
                br.ReadInt32();
                br.ReadInt64();
            }
            br.Close();
            return ar.ToArray();
        }

        private DataFileInfo[] GetDataList()
        {
            Stream TempStream = ExtractWholeFile("data.crc");
            if (TempStream == null) return new DataFileInfo[0];
            BinaryReader br = new BinaryReader(TempStream);
            List<DataFileInfo> ar = new List<DataFileInfo>();
            while (br.PeekChar() != -1)
            {
                string s = br.ReadString();
                ar.Add(new DataFileInfo(s, br.ReadUInt32()));
                br.ReadInt64();
            }
            br.Close();
            return ar.ToArray();
        }

        internal string GetPlugins()
        {
            return ParseCompressedStream("plugins.crc", "plugins");
        }

        internal string GetDataFiles()
        {
            return ParseCompressedStream("data.crc", "data");
        }

        internal string GetReadme()
        {
            Stream s = ExtractWholeFile("readme");
            if (s == null) return null;
            BinaryReader br = new BinaryReader(s);
            string readme = br.ReadString();
            br.Close();
            return readme;
        }

        internal string GetScript()
        {
            Stream s = ExtractWholeFile("script");
            if (s == null) return null;
            BinaryReader br = new BinaryReader(s);
            string script = br.ReadString();
            br.Close();
            return script;
        }

        internal bool DoesReadmeExist()
        {
            ZipEntry ze = ModFile.GetEntry("readme");
            return (ze != null);
        }

        internal bool DoesScriptExist()
        {
            ZipEntry ze = ModFile.GetEntry("script");
            return (ze != null);
        }

        private string ParseCompressedStream(string fileList, string compressedStream)
        {
            string path;
            Stream FileList = ExtractWholeFile(fileList);
            if (FileList == null) return null;
            Stream CompressedStream = ExtractWholeFile(compressedStream);
            path = CompressionHandler.DecompressFiles(FileList, CompressedStream, CompType);
            FileList.Close();
            CompressedStream.Close();
            return path;
        }

        internal string GetConfig()
        {
            string s = "";
            Stream st = ExtractWholeFile("config", ref s);
            st.Close();
            return s;
        }

        private Stream ExtractWholeFile(string s)
        {
            string s2 = null;
            return ExtractWholeFile(s, ref s2);
        }
        private Stream ExtractWholeFile(string s, ref string path)
        {
            ZipEntry ze = ModFile.GetEntry(s);
            if (ze == null) return null;
            return ExtractWholeFile(ze, ref path);
        }
        private Stream ExtractWholeFile(ZipEntry ze, ref string path)
        {
            Stream file = ModFile.GetInputStream(ze);
            Stream TempStream;
            if (path != null || ze.Size > 67108864)
            {
                TempStream = Program.CreateTempFile(out path);
            }
            else
            {
                TempStream = new MemoryStream((int)ze.Size);
            }
            byte[] buffer = new byte[4096];
            int i;
            while ((i = file.Read(buffer, 0, 4096)) > 0)
            {
                TempStream.Write(buffer, 0, i);
            }
            TempStream.Position = 0;
            return TempStream;
        }
    }
}
