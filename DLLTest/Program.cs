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
            string data = @"S:\SteamLibrary\steamapps\common\Oblivion\data";
            f.SetDataDirectory(data);
            f.SetOblivionDirectory(@"S:\SteamLibrary\steamapps\common\Oblivion");
            f.SetOblivionINIPath(@"C:\Users\Florian\Documents\My Games\Oblivion\Oblivion.ini");
            f.SetOBMMVersion(1, 1, 12);
            f.SetOutputDirectory(@"M:\Projects\omod\testDLL\out");
            f.SetPluginsListPath(@"C:\Users\Florian\AppData\Local\Oblivion\Plugins.txt");
            f.SetTempDirectory(@"M:\Projects\omod\testDLL\temp");
            f.SetDLLPath(@"M:\Projects\OMOD-Extractor\OMOD-Framework\bin\Release\erri120.OMODFramework.dll");
            OMOD omod = new OMOD(@"M:\Projects\omod\testDLL\Oblivion XP v415 - OMOD-15619.omod", f);
            Console.WriteLine(omod.ExtractDataFiles());
            Console.WriteLine(omod.ExtractPlugins());
            ScriptRunner sr = new ScriptRunner(omod);
            string[] activeESP = new string[1]{ "oblivion.esm" };
            sr.ExecuteScript(
                (s1)=> { Console.WriteLine(s1); }, //warn
                (s1,s2)=> { return 1; }, //dialogYesNo
                (s1) => { return true; }, //existsFile
                null, // getFileVersion
                (s1,s2,b1,s3,s4)=> { return new int[1] { 1 }; }, //dialogSelect
                null, //message
                null, //displayImage
                null, //displayText
                (s1,s2)=> { return "Hi"; }, //inputString
                ()=> { return activeESP; }, //getActiveESPNames
                (s1) => { return Path.Combine(data,s1); } //getFileFromPath
                );
            Console.WriteLine("hi");
        }
    }
}
