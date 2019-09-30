using System;
using System.IO;
using OMODFramework;
using OMODFramework.Scripting;

namespace DLLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Framework f = new Framework();
            f.SetDataDirectory("");
            f.SetOblivionDirectory("");
            f.SetOblivionINIPath("");
            f.SetOBMMVersion(1, 1, 12);
            f.SetOutputDirectory(@"M:\Projects\omod\testDLL\out");
            f.SetPluginsListPath("");
            f.SetTempDirectory(@"M:\Projects\omod\testDLL\temp");
            OMOD omod = new OMOD(@"M:\Projects\omod\testDLL\DarkUId DarN 16.omod", f);
            Console.WriteLine(omod.ExtractDataFiles());
            Console.WriteLine(omod.ExtractPlugins());
            ScriptRunner sr = new ScriptRunner(omod);
            sr.ExecuteScript(null, null, null, null, null, null, null, null, null, null, null);
            Console.WriteLine("hi");
        }
    }
}
