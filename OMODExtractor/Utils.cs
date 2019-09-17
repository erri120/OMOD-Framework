using System;
using System.Collections.Generic;
using System.IO;

namespace OMODExtractor
{
    public class Utils
    {
        private List<string> tempDirs = new List<string>();

        public Utils() { }

        public void AddTempDir(string dir)
        {
            tempDirs.Add(dir);
        }

        public void Exit(int code)
        {
            Cleanup();
            System.Environment.Exit(code);
        }

        public void Cleanup()
        {
            foreach (var item in tempDirs)
            {
                if (Directory.Exists(item))
                {
                    DirectoryInfo di = new DirectoryInfo(item);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                    Directory.Delete(item);
                }
            }
        }
        public void SaveToFile(string contents, string dest)
        {
            if (File.Exists(dest)) File.Delete(dest);
            File.WriteAllText(dest, contents);
        }
    }
}
