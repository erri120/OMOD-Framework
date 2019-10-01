using System;
using System.Collections.Generic;
using System.IO;

namespace OMODFramework.Disabled
{
    internal static class OblivionINI
    {
        private static readonly string ini = null;//Framework.OblivionINIPath;

        internal static string GetINIValue(string section, string name)
        {
            string[] ss = GetINISection(section);
            if (ss != null)
            {
                name = name.ToLower();
                foreach (string s in ss)
                {
                    if (s.Trim().ToLower().StartsWith(name + "="))
                    {
                        string res = s.Substring(s.IndexOf('=') + 1).Trim();
                        int i = res.IndexOf(';');
                        if (i != -1) res = res.Substring(0, i - 1);
                        return res;
                    }
                }
            }
            return null;
        }

        private static string[] GetINISection(string section)
        {
            List<string> contents = new List<string>();
            bool InSection = false;
            section = section.ToLower();
            StreamReader sr = new StreamReader(File.OpenRead(ini), System.Text.Encoding.Default);
            try
            {
                while (sr.Peek() != -1)
                {
                    string s = sr.ReadLine();
                    if (InSection)
                    {
                        if (s.Trim().StartsWith("[") && s.Trim().EndsWith("]")) break;
                        contents.Add(s);
                    }
                    else
                    {
                        if (s.Trim().ToLower() == section) InSection = true;
                    }
                }
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            if (!InSection) return null;
            return contents.ToArray();
        }
    }
}
