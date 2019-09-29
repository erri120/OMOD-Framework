using System;
using System.IO;
using System.Collections.Generic;
using omod = OblivionModManager.OMOD;

namespace OblivionModManager
{
    [Serializable]
    internal class sData
    {
        /// <summary>
        /// List of all ESPs
        /// </summary>
        internal readonly List<EspInfo> Esps = new List<EspInfo>();
        /// <summary>
        /// List of all INIEdits, do note that this is an OBMM leftover as we don't collect INIEdits from other mods
        /// </summary>
        internal readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
        /// <summary>
        /// List of all OMODs, same as INIEdits: leftover from OBMM
        /// </summary>
        internal readonly List<omod> omods = new List<omod>();
    }
}
