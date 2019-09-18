using System;
using System.IO;

namespace OMODExtractorDLL
{
    public class Utils
    {
        public void DeleteDir(string item)
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

        public static void SaveToFile(string contents, string dest)
        {
            if (File.Exists(dest)) File.Delete(dest);
            File.WriteAllText(dest, contents);
        }

        public static string CreateTempDirectory(string s)
        {
            string tempdir = Path.Combine(Directory.GetCurrentDirectory(),s);
            Console.Write(tempdir);
            for (int i = 0; i < 32000; i++)
            {
                if (!Directory.Exists(Path.Combine(tempdir, i.ToString())) && !File.Exists(Path.Combine(tempdir, i.ToString())))
                {
                    Console.WriteLine(Path.Combine(tempdir,i.ToString()));
                    Directory.CreateDirectory(Path.Combine(tempdir,i.ToString()));
                    return Path.Combine(tempdir, i.ToString())+"\\";
                }
            }
            throw new Exception("Could not create temp folder because directory is full");
        }
    }
}
