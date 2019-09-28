using System;
using System.Collections.Generic;

namespace OblivionModManager
{
    internal enum CompressionType : byte { SevenZip, Zip }
    internal enum CompressionLevel : byte { VeryHigh, High, Medium, Low, VeryLow, None }
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }
    public enum DeactiveStatus { Allow, WarnAgainst, Disallow }
    internal enum ScriptType { obmmScript, Python, cSharp, vb, Count }

    internal struct ScriptEspWarnAgainst { }

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

    internal class ScriptReturnData
    {
        internal readonly List<string> IgnorePlugins = new List<string>();
        internal readonly List<string> InstallPlugins = new List<string>();
        internal bool InstallAllPlugins = true;
        internal readonly List<string> IgnoreData = new List<string>();
        internal readonly List<string> InstallData = new List<string>();
        internal bool InstallAllData = true;
        internal readonly List<string> RegisterBSAList = new List<string>();
        internal bool CancelInstall = false;
        internal readonly List<string> UncheckedPlugins = new List<string>();
        internal readonly List<ScriptEspWarnAgainst> EspDeactivation = new List<ScriptEspWarnAgainst>();
        internal readonly List<ScriptCopyDataFile> CopyDataFiles = new List<ScriptCopyDataFile>();
        internal readonly List<ScriptCopyDataFile> CopyPlugins = new List<ScriptCopyDataFile>();
        //internal readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
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
        internal omod Parent;

        internal EspInfo(string fileName, omod parent)
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
}
