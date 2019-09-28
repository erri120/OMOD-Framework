using System;
using System.Collections.Generic;
using System.IO;

namespace OblivionModManager
{
    internal static class OblivionESP
    {
        private static readonly string espfile = Program.OblivionESPDir + "plugins.txt";
        private const string bashespfile = @"obmm\loadorder.txt";
    }

    internal static class OblivionINI
    {
        private static readonly string ini = Program.OblivionINIDir + "oblivion.ini";
        private static readonly string outputIni = Program.UseOutputDir ? Program.OutputDir + "oblivion.ini" : ini;

        internal static string GetINIValue(string section, string name)
        {
            string[] ss = GetINISection(section);
            //if (ss == null) throw new Exception("Oblivion.ini section " + section + " does not exist");
            if(ss != null)
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

        internal static void WriteINIValue(string section, string name, string value)
        {
            List<string> ss = new List<string>(GetINISection(section));
            //if (ss == null) throw new Exception("Oblivion.ini section " + section + " does not exist");
            if(ss != null)
            {
                bool matched = false;
                string lname = name.ToLower();
                for (int i = 0; i < ss.Count; i++)
                {
                    string s = ss[i];
                    if (s.Trim().ToLower().StartsWith(lname + "="))
                    {
                        if (value == null)
                        {
                            ss.RemoveAt(i--);
                        }
                        else
                        {
                            ss[i] = name + "=" + value;

                        }
                        matched = true;
                        break;
                    }
                }
                if (!matched) ss.Add(name + "=" + value);
                ReplaceINISection(section, ss.ToArray());
            }
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

        private static void ReplaceINISection(string section, string[] ReplaceWith)
        {
            List<string> contents = new List<string>();
            StreamReader sr = new StreamReader(File.OpenRead(ini), System.Text.Encoding.Default);
            try
            {
                section = section.ToLower();
                bool InSection = false;
                while (sr.Peek() != -1)
                {
                    string s = sr.ReadLine();
                    if (!InSection)
                    {
                        contents.Add(s);
                        if (s.Trim().ToLower() == section)
                        {
                            InSection = true;
                            contents.AddRange(ReplaceWith);
                        }
                    }
                    else
                    {
                        if (s.Trim().StartsWith("[") && s.Trim().EndsWith("]"))
                        {
                            contents.Add(s);
                            InSection = false;
                        }
                    }
                }
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            StreamWriter sw = new StreamWriter(File.Create(outputIni), System.Text.Encoding.Default);
            try
            {
                foreach (string s in contents)
                {
                    sw.WriteLine(s);
                }
            }
            finally
            {
                if (sw != null) sw.Close();
            }
        }
    }
}
