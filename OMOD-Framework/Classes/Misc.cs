using System;
using System.Collections.Generic;

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

    public class ScriptReturnData
    {
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
        public readonly List<string> InstallData = new List<string>();
        /// <summary>
        /// List with absolute paths of all plugins to be installed
        /// </summary>
        public readonly List<string> InstallPlugins = new List<string>();
        /// <summary>
        /// List with absolute paths of all data files not to be installed
        /// </summary>
        public readonly List<string> IgnoreData = new List<string>();
        /// <summary>
        /// List with absolute paths of all plguins not to be installed
        /// </summary>
        public readonly List<string> IgnorePlugins = new List<string>();
        /// <summary>
        /// Destination of all data files to be copied
        /// </summary>
        public readonly List<ScriptCopyDataFile> CopyDataFiles = new List<ScriptCopyDataFile>();
        /// <summary>
        /// Destination of all plugins to be copied
        /// </summary>
        public readonly List<ScriptCopyDataFile> CopyPlugins = new List<ScriptCopyDataFile>();
        public readonly List<PluginLoadInfo> LoadOrderList = new List<PluginLoadInfo>();
        public readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
        public readonly List<SDPEditInfo> SDPEdits = new List<SDPEditInfo>();
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

        /// <summary>
        /// Struct with information about what plugin to load after/early than another
        /// </summary>
        /// <param name="plugin">The plugin</param>
        /// <param name="target">The target that needs to be loaded after/earlier than the plugin</param>
        /// <param name="loadAfter">Load after or earlier</param>
        public PluginLoadInfo(string plugin, string target, bool loadAfter)
        {
            Plugin = plugin;
            Target = target;
            LoadAfter = loadAfter;
        }
    }

    [Serializable]
    public class INIEditInfo
    {
        public readonly string Section;
        public readonly string Name;
        public string OldValue;
        public readonly string NewValue;
        public OMOD Plugin;

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
