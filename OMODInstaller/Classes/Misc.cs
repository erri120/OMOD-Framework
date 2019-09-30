using System;
using omod = OblivionModManager.OMOD;
using System.Collections.Generic;

namespace OblivionModManager
{
    internal enum CompressionType : byte { SevenZip, Zip }
    internal enum CompressionLevel : byte { VeryHigh, High, Medium, Low, VeryLow, None }
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }
    public enum DeactiveStatus { Allow, WarnAgainst, Disallow }
    internal enum ScriptType { obmmScript, Python, cSharp, vb, Count }

    internal struct ScriptEspWarnAgainst
    {
        internal string Plugin;
        internal DeactiveStatus Status;

        internal ScriptEspWarnAgainst(string plugin, DeactiveStatus status)
        {
            Plugin = plugin;
            Status = status;
        }
    }

    internal struct ScriptCopyDataFile
    {
        internal readonly string CopyFrom;
        internal readonly string CopyTo;
        internal readonly string hCopyFrom;
        internal readonly string hCopyTo;
        internal ScriptCopyDataFile(string from, string to)
        {
            CopyFrom = from.ToLower();
            CopyTo = to.ToLower();
            hCopyFrom = from;
            hCopyTo = to;
        }
    }

    internal struct PluginLoadInfo
    {
        internal string Plugin;
        internal string Target;
        internal bool LoadAfter;

        internal PluginLoadInfo(string plugin, string target, bool loadAfter)
        {
            Plugin = plugin;
            Target = target;
            LoadAfter = loadAfter;
        }
    }

    internal struct ScriptEspEdit
    {
        internal readonly bool IsGMST;
        internal readonly string Plugin;
        internal readonly string EDID;
        internal readonly string Value;

        internal ScriptEspEdit(bool gmst, string plugin, string edid, string value)
        {
            IsGMST = gmst;
            Plugin = plugin;
            EDID = edid;
            Value = value;
        }
    }

    internal class ScriptReturnData
    {
        internal readonly List<string> IgnorePlugins = new List<string>();
        internal readonly List<string> testList = new List<string>();
        internal readonly List<string> InstallPlugins = new List<string>();
        internal bool InstallAllPlugins = true;
        internal readonly List<string> IgnoreData = new List<string>();
        internal readonly List<string> InstallData = new List<string>();
        internal bool InstallAllData = true;
        internal readonly List<PluginLoadInfo> LoadOrderList = new List<PluginLoadInfo>();
        internal readonly List<ConflictData> ConflictsWith = new List<ConflictData>();
        internal readonly List<ConflictData> DependsOn = new List<ConflictData>();
        internal readonly List<string> RegisterBSAList = new List<string>();
        internal bool CancelInstall = false;
        internal readonly List<string> UncheckedPlugins = new List<string>();
        internal readonly List<ScriptEspWarnAgainst> EspDeactivation = new List<ScriptEspWarnAgainst>();
        internal readonly List<ScriptCopyDataFile> CopyDataFiles = new List<ScriptCopyDataFile>();
        internal readonly List<ScriptCopyDataFile> CopyPlugins = new List<ScriptCopyDataFile>();
        internal readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
        internal readonly List<SDPEditInfo> SDPEdits = new List<SDPEditInfo>();
    }

    [Serializable]
    internal struct ConflictData
    {
        internal ConflictLevel level;
        internal string File;
        internal int MinMajorVersion;
        internal int MinMinorVersion;
        internal int MaxMajorVersion;
        internal int MaxMinorVersion;
        internal string Comment;
        internal bool Partial;

        public static bool operator ==(ConflictData cd, omod o)
        {
            if (!cd.Partial && cd.File != o.ModName) return false;
            if (cd.Partial)
            {
                System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(cd.File, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (!reg.IsMatch(o.ModName)) return false;
            }
            if (cd.MaxMajorVersion != 0 || cd.MaxMinorVersion != 0)
            {
                if (cd.MaxMajorVersion > o.MajorVersion) return false;
                if (cd.MaxMajorVersion == o.MajorVersion && cd.MaxMinorVersion > o.MinorVersion) return false;
            }
            if (cd.MinMajorVersion != 0 || cd.MinMinorVersion != 0)
            {
                if (cd.MinMajorVersion < o.MajorVersion) return false;
                if (cd.MinMajorVersion == o.MajorVersion && cd.MinMinorVersion < o.MinorVersion) return false;
            }
            return true;
        }
        public static bool operator !=(ConflictData cd, omod o)
        {
            return !(cd == o);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is ConflictData)
            {
                ConflictData cd = (ConflictData)obj;
                if (File == cd.File && MinMajorVersion == cd.MinMajorVersion && MinMinorVersion == cd.MinMinorVersion &&
                    MaxMajorVersion == cd.MaxMajorVersion && MaxMinorVersion == cd.MaxMinorVersion && Comment == cd.Comment)
                {
                    return true;
                }
            }
            else if (obj is omod)
            {
                return (this == (omod)obj);
            }
            return false;
        }
    }

    internal class ScriptExecutationData
    {
        internal PluginLoadInfo[] PluginOrder;
        internal string[] UncheckedPlugins;
        internal ScriptEspWarnAgainst[] EspDeactivationWarning;
        internal ScriptEspEdit[] EspEdits;
        internal string[] EarlyPlugins;
    }

    [Serializable]
    internal class DataFileInfo
    {
        internal readonly string FileName;
        internal readonly string LowerFileName;
        internal uint CRC;
        private readonly List<string> UsedBy = new List<string>();

        public static bool operator ==(DataFileInfo a, DataFileInfo b)
        {
            if (null == (object)a || null == (object)b)
            {
                if (null != (object)a || null != (object)b) return false; else return true;
            }
            return (a.LowerFileName == b.LowerFileName);
        }
        public static bool operator !=(DataFileInfo a, DataFileInfo b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is DataFileInfo)) return false;
            return this == (DataFileInfo)obj;
        }
        public override int GetHashCode() { return LowerFileName.GetHashCode(); }

        //I dont really want this to be here, but .NET does some stupid implicit convertion by calling ToString() it isn't
        /*internal static implicit operator string(DataFileInfo dfi) {
            return dfi.FileName;
        }*/

        public override string ToString()
        {
            return FileName;
        }

        internal DataFileInfo(string s, uint crc)
        {
            FileName = s;
            LowerFileName = FileName.ToLower();
            CRC = crc;
        }
        internal DataFileInfo(DataFileInfo orig)
        {
            FileName = orig.FileName;
            LowerFileName = orig.LowerFileName;
            CRC = orig.CRC;
        }

        internal string Owners { get { return string.Join(", ", UsedBy.ToArray()); } }
    }

    [Serializable]
    internal class EspInfo
    {
        internal const string UnknownOwner = "Unknown";
        internal const string BaseOwner = "Base";
        internal static string[] BaseFiles = { "oblivion.esm" };

        internal readonly string FileName;
        internal readonly string LowerFileName;
        internal string BelongsTo;
        internal bool Active;
        internal OMOD Parent;

        internal EspInfo(string fileName, OMOD parent)
        {
            FileName = fileName;
            LowerFileName = FileName.ToLower();
            Parent = parent;
        }

        internal EspInfo(string fileName)
        {
            FileName = fileName;
            LowerFileName = FileName.ToLower();
            if (System.Array.IndexOf<string>(BaseFiles, LowerFileName) != -1)
            {
                BelongsTo = BaseOwner;
            }
            else
            {
                BelongsTo = UnknownOwner;
            }
            Parent = null;
        }
    }

    [Serializable]
    internal class INIEditInfo
    {
        internal readonly string Section;
        internal readonly string Name;
        internal string OldValue;
        internal readonly string NewValue;
        internal OMOD Plugin;

        internal INIEditInfo(string section, string name, string newvalue)
        {
            Section = section.ToLower();
            Name = name.ToLower();
            NewValue = newvalue;
        }

        public static bool operator ==(INIEditInfo a, INIEditInfo b) { return (a.Section == b.Section) && (a.Name == b.Name); }
        public static bool operator !=(INIEditInfo a, INIEditInfo b) { return (a.Section != b.Section) || (a.Name != b.Name); }
        public override bool Equals(object obj) { return this == (obj as INIEditInfo); }
        public override int GetHashCode() { return Section.GetHashCode() + Name.GetHashCode(); }
    }

    [Serializable]
    internal class SDPEditInfo
    {
        internal readonly byte Package;
        internal readonly string Shader;
        internal string BinaryObject;

        internal SDPEditInfo(byte package, string shader, string binaryObject)
        {
            Package = package;
            Shader = shader.ToLower();
            BinaryObject = binaryObject.ToLower();
        }
    }
}
