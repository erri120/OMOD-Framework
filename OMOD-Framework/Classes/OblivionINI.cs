using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OMODFramework.Disabled
{
    internal static class OblivionINI
    {
        private static readonly string ini = null;//Framework.OblivionINIPath;

/*
        internal static string GetINIValue(string section, string name)
        {
            string[] ss = GetINISection(section);
            if (ss != null)
            {
                name = name.ToLower();
                foreach (var s in ss)
                {
                    if (s.Trim().ToLower().StartsWith(name + "="))
                    {
                        var res = s.Substring(s.IndexOf('=') + 1).Trim();
                        var i = res.IndexOf(';');
                        if (i != -1) res = res.Substring(0, i - 1);
                        return res;
                    }
                }
            }
            return null;
        }
*/

        private static string[] GetINISection(string section)
        {
            var contents = new List<string>();
            var InSection = false;
            section = section.ToLower();
            var sr = new StreamReader(File.OpenRead(ini), Encoding.Default);
            try
            {
                while (sr.Peek() != -1)
                {
                    var s = sr.ReadLine();
                    if (InSection)
                    {
                        if (s != null && (s.Trim().StartsWith("[") && s.Trim().EndsWith("]"))) break;
                        contents.Add(s);
                    }
                    else
                    {
                        if (s != null && s.Trim().ToLower() == section) InSection = true;
                    }
                }
            }
            finally
            {
                sr.Close();
            }
            if (!InSection) return null;
            return contents.ToArray();
        }
    }
}
