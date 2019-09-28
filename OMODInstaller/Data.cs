using System;
using System.IO;
using System.Collections.Generic;
using omod = OblivionModManager.OMOD;

namespace OblivionModManager
{
    [Serializable]
    internal class sData
    {
        internal readonly List<EspInfo> Esps = new List<EspInfo>();
        internal readonly List<INIEditInfo> INIEdits = new List<INIEditInfo>();
        internal readonly List<omod> omods = new List<omod>();
    }
}
