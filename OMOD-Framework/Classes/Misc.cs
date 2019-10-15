using System;
using System.Collections.Generic;
using System.IO;

namespace OMODFramework
{
    /// <summary>
    /// Type used to compress data.crc and plugins.crc
    /// </summary>
    public enum CompressionType : byte { SevenZip, Zip }
    /// <summary>
    /// Conflict level between files
    /// </summary>
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }
    /// <summary>
    /// The script type of the OMOD installation script
    /// </summary>
    public enum ScriptType { obmmScript, Python, cSharp, vb, Count }
    /// <summary>
    /// Disabled in the script but still needed for compiling
    /// </summary>
    public enum DeactiveStatus { Allow, WarnAgainst, Disallow }

    public class ScriptReturnData
    {
        /// <summary>
        /// Populated after calling srd = Framework.Pretty
        /// </summary>
        public List<InstallFile> InstallFiles = new List<InstallFile>();
        /// <summary>
        /// If the installation is canceled
        /// </summary>
        public bool CancelInstall = false;
        /// <summary>
        /// If all data files are to be installed
        /// </summary>
        public bool InstallAllData = true;
        /// <summary>
        /// If all plugins files are to be installed
        /// </summary>
        public bool InstallAllPlugins = true;
        /// <summary>
        /// List with absolute paths of all data files to be installed
        /// </summary>
        public List<string> InstallData = new List<string>();
        /// <summary>
        /// List with absolute paths of all plugins to be installed
        /// </summary>
        public List<string> InstallPlugins = new List<string>();
        /// <summary>
        /// List with absolute paths of all data files not to be installed
        /// </summary>
        public List<string> IgnoreData = new List<string>();
        /// <summary>
        /// List with absolute paths of all plguins not to be installed
        /// </summary>
        public List<string> IgnorePlugins = new List<string>();
        /// <summary>
        /// Destination of all data files to be copied
        /// </summary>
        public List<ScriptCopyDataFile> CopyDataFiles = new List<ScriptCopyDataFile>();
        /// <summary>
        /// Destination of all plugins to be copied
        /// </summary>
        public List<ScriptCopyDataFile> CopyPlugins = new List<ScriptCopyDataFile>();
        /// <summary>
        /// Lists with all Loader Order Information
        /// </summary>
        public readonly List<PluginLoadInfo> LoadOrderList = new List<PluginLoadInfo>();
        /// <summary>
        /// Lists with all INIEdits
        /// </summary>
        public readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
        /// <summary>
        /// Lists with all Shader Edits
        /// </summary>
        public readonly List<SDPEditInfo> SDPEdits = new List<SDPEditInfo>();

        /// <summary>
        /// Makes a pretty output of the ScriptReturnData. Populates the InstallFiles list
        /// and does a lot of background sorting and matching for you. Will return null
        /// if CancelInstall is true
        /// </summary>
        /// <param name="copy">Wether or not to also do the CopyData/Plugins methods</param>
        /// <param name="omod">The OMOD file</param>
        /// <param name="dataPath">The dataPath with extracted data files</param>
        /// <param name="pluginsPath">The pluginsPath with extracted plugins files</param>
        /// <returns></returns>
        public ScriptReturnData Pretty(bool copy, ref OMOD omod, ref string pluginsPath, ref string dataPath)
        {
            if (CancelInstall) return null;

            var filesPlugins = new List<string>();
            var filesData = new List<string>();

            if (InstallAllPlugins) foreach (var s in omod.GetPluginList()) if (!s.Contains("\\")) filesPlugins.Add(s);
            foreach (var s in InstallPlugins) { if (!Framework.strArrayContains(filesPlugins, s)) filesPlugins.Add(s); }
            foreach (var s in IgnorePlugins) { Framework.strArrayRemove(filesPlugins, s); }

            if (copy)
            {
                foreach (var scd in CopyPlugins)
                {
                    if (!File.Exists(Path.Combine(pluginsPath, scd.CopyFrom))) break;
                    if (scd.CopyFrom != scd.CopyTo)
                    {
                        if (File.Exists(Path.Combine(pluginsPath, scd.CopyTo))) File.Delete(Path.Combine(pluginsPath, scd.CopyTo));
                        File.Copy(Path.Combine(pluginsPath, scd.CopyFrom), Path.Combine(pluginsPath, scd.CopyTo));
                    }
                    if (!Framework.strArrayContains(filesPlugins, scd.CopyTo)) filesPlugins.Add(scd.CopyTo);
                }
            }

            if (InstallAllData)
            {
                foreach (var s in omod.GetDataFileList()) { filesData.Add(s); }
            }
            foreach (var s in InstallData) { if (!Framework.strArrayContains(filesData, s)) filesData.Add(s); }
            foreach (var s in IgnoreData) { Framework.strArrayRemove(filesData, s); }

            if (copy)
            {
                foreach (var scd in CopyDataFiles)
                {
                    if (!File.Exists(Path.Combine(dataPath, scd.CopyFrom))) break;
                    if (scd.CopyFrom != scd.CopyTo)
                    {
                        var dirName = Path.GetDirectoryName(Path.Combine(dataPath, scd.CopyTo));
                        if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
                        if (File.Exists(Path.Combine(dataPath, scd.CopyTo))) File.Delete(Path.Combine(dataPath, scd.CopyTo));
                        File.Copy(Path.Combine(dataPath, scd.CopyFrom), Path.Combine(dataPath, scd.CopyTo));
                    }
                    if (!Framework.strArrayContains(filesData, scd.CopyTo)) filesData.Add(scd.CopyTo);
                }
            }

            for (var i = 0; i < filesData.Count; i++)
            {
                if (filesData[i].StartsWith("\\")) filesData[i] = filesData[i].Substring(1);
                var currentFile = Path.Combine(dataPath, filesData[i]);
                if (!File.Exists(currentFile)) filesData.RemoveAt(i--);
            }

            for (var i = 0; i < filesPlugins.Count; i++)
            {
                if (filesPlugins[i].StartsWith("\\")) filesPlugins[i] = filesPlugins[i].Substring(1);
                var currentFile = Path.Combine(pluginsPath, filesPlugins[i]);
                if (!File.Exists(currentFile)) filesPlugins.RemoveAt(i--);
            }

            foreach (var s in filesData) InstallFiles.Add(new InstallFile(Path.Combine(dataPath, s), s));
            foreach (var s in filesPlugins) InstallFiles.Add(new InstallFile(Path.Combine(pluginsPath, s),s));

            InstallAllData = false;
            InstallAllPlugins = false;
            InstallData = null;
            InstallPlugins = null;
            IgnoreData = null;
            IgnorePlugins = null;
            if (copy)
            {
                CopyDataFiles = null;
                CopyPlugins = null;
            }


            return this;
        }
    }

    public struct InstallFile
    {
        public readonly string InstallFrom;
        public readonly string InstallTo;
        public InstallFile(string from, string to)
        {
            InstallFrom = from;
            InstallTo = to;
        }
    }

    public struct ScriptCopyDataFile
    {
        public readonly string CopyFrom;
        public readonly string CopyTo;
        /// <summary>
        /// Struct with information about what data file to copy where to
        /// </summary>
        /// <param name="from">The Data file to copy from the temp folder</param>
        /// <param name="to">The path to the file to copy to</param>
        public ScriptCopyDataFile(string from, string to)
        {
            CopyFrom = from;
            CopyTo = to;
        }
    }

    public struct PluginLoadInfo
    {
        public string Plugin;
        public string Target;
        public bool LoadAfter;
        public bool Early;

        /// <summary>
        /// Struct with information about what plugin to load after/early than another
        /// </summary>
        /// <param name="plugin">The plugin</param>
        /// <param name="target">The target that needs to be loaded after/earlier than the plugin</param>
        /// <param name="loadAfter">Load after or earlier</param>
        /// <param name="early">Whether to load early</param>
        public PluginLoadInfo(string plugin, string target, bool loadAfter, bool early)
        {
            Plugin = plugin;
            Target = target;
            LoadAfter = loadAfter;
            Early = early;
        }
    }

    [Serializable]
    public class INIEditInfo
    {
        public readonly string Section;
        public readonly string Name;
        public readonly string NewValue;

        /// <summary>
        /// INIEditInfo is used for creating ini tweaks
        /// </summary>
        /// <param name="section">The section name with out the []</param>
        /// <param name="name">The name of the item</param>
        /// <param name="newvalue">The new value the item should have</param>
        public INIEditInfo(string section, string name, string newvalue)
        {
            Section = section.ToLower();
            Name = name.ToLower();
            NewValue = newvalue;
        }
    }

    [Serializable]
    public class SDPEditInfo
    {
        public readonly byte Package;
        public readonly string Shader;
        public string BinaryObject;

        /// <summary>
        /// Shader edits
        /// </summary>
        /// <param name="package">The package</param>
        /// <param name="shader">The shader</param>
        /// <param name="binaryObject">The binary object</param>
        public SDPEditInfo(byte package, string shader, string binaryObject)
        {
            Package = package;
            Shader = shader.ToLower();
            BinaryObject = binaryObject.ToLower();
        }
    }
}
