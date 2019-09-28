﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace OblivionModManager
{
    internal static class Program
    {
        internal const string version = "1.1.12"; // latest official obmm release
        internal const byte MajorVersion = 1;
        internal const byte MinorVersion = 1;
        internal const byte BuildNumber = 12;
        internal const byte CurrentOmodVersion = 4;

        internal static string TempDir { get; set; } = Path.GetTempPath() + @"obmm\";

        internal static string CurrentDir { get; set; }

        internal static string OblivionINIDir { get; set; }

        internal static string OblivionESPDir { get; set; }

        internal static string OutputDir { get; set; }
        internal static bool UseOutputDir { get; set; } = false;

        internal static string EXEFile { get; set; }
        internal static bool UseEXE { get; set; } = false;

        internal static sData Data = new sData();

        internal static bool IsSafeFileName(string s)
        {
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[s.Length - 1]) != -1) return false;
            return true;
        }

        internal static bool IsSafeFolderName(string s)
        {
            if (s.Length == 0) return true;
            s = s.Replace('/', '\\');
            if (s.IndexOfAny(Path.GetInvalidPathChars()) != -1) return false;
            if (Path.IsPathRooted(s)) return false;
            if (s.StartsWith(".") || Array.IndexOf<char>(Path.GetInvalidFileNameChars(), s[0]) != -1) return false;
            if (s.Contains("\\..\\")) return false;
            if (s.EndsWith(".")) return false;
            return true;
        }

        internal static bool strArrayContains(List<string> a, string s)
        {
            s = s.ToLower();
            foreach (string s2 in a)
            {
                if (s2.ToLower() == s) return true;
            }
            return false;
        }
        internal static bool strArrayContains(List<DataFileInfo> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].LowerFileName == s) return true;
            }
            return false;
        }

        internal static DataFileInfo strArrayGet(DataFileInfo[] a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].LowerFileName == s) return a[i];
            }
            return null;
        }

        internal static void strArrayRemove(List<string> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].ToLower() == s)
                {
                    a.RemoveAt(i);
                    return;
                }
            }
        }
        internal static void strArrayRemove(List<DataFileInfo> a, string s)
        {
            s = s.ToLower();
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].LowerFileName == s)
                {
                    a.RemoveAt(i);
                    return;
                }
            }
        }

        internal static FileStream CreateTempFile()
        {
            string s;
            return CreateTempFile(out s);
        }
        internal static FileStream CreateTempFile(out string path)
        {
            int i = 0;
            for (i = 0; i < 32000; i++)
            {
                if (!File.Exists(TempDir + "tmp" + i.ToString()))
                {
                    path = TempDir + "tmp" + i.ToString();
                    return File.Create(path);
                }
            }
            throw new Exception("Could not create temp file because directory is full");
        }

        internal static string CreateTempDirectory()
        {
            for (int i = 0; i < 32000; i++)
            {
                if (!Directory.Exists(TempDir + i.ToString()))
                {
                    Directory.CreateDirectory(TempDir + i.ToString() + "\\");
                    return TempDir + i.ToString() + "\\";
                }
            }
            throw new Exception("Could not create temp folder because directory is full");
        }

        internal static void ClearTempFiles() { ClearTempFiles(""); }
        internal static void ClearTempFiles(string subfolder)
        {
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
            if (!Directory.Exists(TempDir + subfolder)) return;
            foreach (string file in Directory.GetFiles(TempDir + subfolder))
            {
                try { File.Delete(file); } catch { }
            }
            try { Directory.Delete(TempDir + subfolder, true); } catch { }
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);
        }

        internal static string ReadAllText(string file)
        {
            if (!File.Exists(file)) return null;
            return File.ReadAllText(file, System.Text.Encoding.Default);
        }

        internal static string ReadBString(BinaryReader br, int len)
        {
            string s = "";
            byte[] bs = br.ReadBytes(len);
            foreach (byte b in bs) s += (char)b;
            return s;
        }
        internal static string ReadCString(BinaryReader br)
        {
            string s = "";
            while (true)
            {
                byte b = br.ReadByte();
                if (b == 0) return s;
                s += (char)b;
            }
        }
    }
}
