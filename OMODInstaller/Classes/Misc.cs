using System;
using System.Collections.Generic;

namespace OblivionModManager
{
    public enum ConflictLevel { Active, NoConflict, MinorConflict, MajorConflict, Unusable }
    public enum DeactiveStatus { Allow, WarnAgainst, Disallow }

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
}
