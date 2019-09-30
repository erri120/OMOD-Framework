using System;
using System.Collections.Generic;
using System.IO;

namespace OMODFramework
{
    internal static class OblivionINI
    {
        private static readonly string ini = Framework.OblivionINIPath;

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

        private static void CreateINITweak(string section, string name, string value)
        {
            string sectionName = section.Replace("[", "").Replace("]", "");
            List<string> contents = new List<string>();
            string tweakPath = Path.Combine(Framework.OutputDir, sectionName + "_tweak.ini");
            if (File.Exists(tweakPath))
            {
                using (StreamReader sr = new StreamReader(tweakPath, System.Text.Encoding.Default))
                {
                    while (sr.Peek() != -1)
                    {
                        string s = sr.ReadLine();
                        contents.Add(s);
                    }
                }
            }
            using (StreamWriter sw = new StreamWriter(File.Create(tweakPath), System.Text.Encoding.Default))
            {
                if (!contents.Contains(section)) contents.Add(section);
                contents.Add(name + "=" + value);
                foreach (string s in contents)
                {
                    sw.WriteLine(s);
                }
            }
        }
    }
}
