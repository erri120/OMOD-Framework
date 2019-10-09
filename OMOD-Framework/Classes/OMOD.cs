using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using OMODFramework.Classes;

namespace OMODFramework
{
    public class OMOD
    {
        private readonly Framework framework;

        protected class PrivateData
        {
            internal ZipFile modFile;
        }

        [NonSerialized]
        private PrivateData pD = new PrivateData();
/*
        internal void RecreatePrivateData() { if (pD == null) pD = new PrivateData(); }
*/

        internal string FilePath;
        internal string FileName;
        internal string LowerFileName;
        internal string ModName;
        internal int MajorVersion;
        internal int MinorVersion;
        internal int BuildVersion;
        internal string Description;
        internal string Email;
        internal string Website;
        internal string Author;
        internal string[] AllPlugins;
        internal string[] AllDataFiles;
        internal CompressionType CompType;
        private readonly byte FileVersion;

        internal string Version => "" + MajorVersion + ((MinorVersion != -1) ? ("." + MinorVersion + ((BuildVersion != -1) ? ("." + BuildVersion) : "")) : "");
        internal string FullFilePath => Path.Combine(FilePath, FileName);

        private ZipFile ModFile
        {
            get
            {
                if (pD.modFile != null) return pD.modFile;
                pD.modFile = new ZipFile(FullFilePath);
                return pD.modFile;
            }
        }

        /// <summary>
        /// OMOD
        /// </summary>
        /// <param name="path">Absolute path to the .omod file</param>
        /// <param name="f">The Framework</param>
        public OMOD(string path, ref Framework f)
        {
            framework = f;
            FilePath = Path.GetDirectoryName(Path.GetFullPath(path));
            FileName = Path.GetFileName(path);
            LowerFileName = FileName.ToLower();

            using (var Config = ExtractWholeFile("config"))
            using (var br = new BinaryReader(Config))
            {
                var version = br.ReadByte();
                FileVersion = version;
                if (version > framework.OBMMFakeCurrentOmodVersion) throw new Exception($"The OMOD version is greater than the set fake OMOD version: {version}>{framework.OBMMFakeCurrentOmodVersion}");

                ModName = br.ReadString();
                MajorVersion = br.ReadInt32();
                MinorVersion = br.ReadInt32();
                Author = br.ReadString();
                Email = br.ReadString();
                Website = br.ReadString();
                Description = br.ReadString();
                if (version >= 2)
                {
                    DateTime.FromBinary(br.ReadInt64());
                }
                else
                {
                    br.ReadString();
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

        #region API Functions
        /// <summary>
        /// Checks if the omod contains a readme file
        /// </summary>
        /// <returns></returns>
        public bool HasReadme() { return ModFile.GetEntry("readme") != null; }
        /// <summary>
        /// Checks if the omod contains a config file
        /// </summary>
        /// <returns></returns>
        public bool HasScript() { return ModFile.GetEntry("config") != null; }
        /// <summary>
        /// Returns the OMOD version
        /// </summary>
        /// <returns>Possible returns: 1 | 2 | 3 | 4</returns>
        public byte GetFileVersion() { return FileVersion; }
        /// <summary>
        /// Returns the name of the mod
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetModName() { return ModName; }
        /// <summary>
        /// Returns the version of the mod, syntax: Major.Minor.Build
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetVersion() { return $"{MajorVersion}.{MinorVersion}.{BuildVersion}"; }
        /// <summary>
        /// Returns the description of the mod
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetDescription() { return Description; }
        /// <summary>
        /// Returns the email of the author
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetEmail() { return Email; }
        /// <summary>
        /// Returns the website of the mod/author
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetWebsite() { return Website; }
        /// <summary>
        /// Returns the name of the author
        /// </summary>
        /// <returns>Returns an empty string if the you didn't read the config or the field is empty</returns>
        public string GetAuthor() { return Author; }
        /// <summary>
        /// Extracts the all plugins from plugins.crc and returns their path
        /// </summary>
        /// <returns>Path to all plugins from plugins.crc</returns>
        public string ExtractPlugins() { return ParseCompressedStream("plugins.crc", "plugins"); }
        /// <summary>
        /// Extracts the data files from data.crc and returns its path
        /// </summary>
        /// <returns>Path to all data files from data.crc</returns>
        public string ExtractDataFiles() { return ParseCompressedStream("data.crc", "data"); }
        /// <summary>
        /// Returns a list of all plugins found in the plugins.crc file
        /// </summary>
        /// <returns></returns>
        public string[] GetPluginList() { return GetList("plugins.crc"); }
        /// <summary>
        /// Returns a list of all data files found in the data.crc file
        /// </summary>
        /// <returns></returns>
        public string[] GetDataFileList() { return GetList("data.crc"); }
        private string[] GetList(string s)
        {
            var TempStream = ExtractWholeFile(s);
            if (TempStream == null) return new string[0];
            using (var br = new BinaryReader(TempStream))
            {
                var ar = new List<string>();
                while (br.PeekChar() != -1)
                {
                    ar.Add(br.ReadString());
                    br.ReadInt32();
                    br.ReadInt64();
                }
                return ar.ToArray();
            }
        }
        #endregion

        #region OBMM-OMOD Functions
        internal Framework GetFramework() { return framework; }
        internal string GetScript()
        {
            using (var Script = ExtractWholeFile("script"))
            using (var br = new BinaryReader(Script))
            {
                var script = br.ReadString();
                return script;
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
        private string[] GetDataList()
        {
            using (var TempStream = ExtractWholeFile("data.crc"))
            using (var br = new BinaryReader(TempStream))
            {
                var ar = new List<string>();
                while (br.PeekChar() != -1)
                {
                    var s = br.ReadString();
                    br.ReadUInt32();
                    br.ReadInt64();
                    ar.Add(s);
                }
                return ar.ToArray();
            }
        }
        private string ParseCompressedStream(string fileList, string compressedStream)
        {
            string path;
            var FileList = ExtractWholeFile(fileList);
            if (FileList == null) return null;
            using (var CompressedStream = ExtractWholeFile(compressedStream))
            {
                path = CompressionHandler.DecompressFiles(FileList, CompressedStream, CompType);
            }
            return path;
        }

        private Stream ExtractWholeFile(string s)
        {
            string s2 = null;
            return ExtractWholeFile(s, ref s2);
        }

        private Stream ExtractWholeFile(string s, ref string path)
        {
            var ze = ModFile.GetEntry(s);
            if (ze == null) return null;
            return ExtractWholeFile(ze, ref path);
        }

        private Stream ExtractWholeFile(ZipEntry ze, ref string path)
        {
            using (var file = ModFile.GetInputStream(ze))
            {
                Stream TempStream;
                if (path != null || ze.Size > 67108864) TempStream = Framework.CreateTempFile(out path);
                else TempStream = new MemoryStream((int)ze.Size);
                var buffer = new byte[4096];
                int i;
                while ((i = file.Read(buffer, 0, 4096)) > 0)
                {
                    TempStream.Write(buffer, 0, i);
                }
                TempStream.Position = 0;
                return TempStream;
            }
        }
        #endregion
    }
}
