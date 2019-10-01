using System;
using System.Diagnostics;
using System.IO;
using OMODFramework;
using OMODFramework.Scripting;
namespace OMOD_Framework_Example
{
    class Program
    {
        internal class ScriptFunctions : IScriptRunnerFunctions
        {
            public int[] DialogSelect(string[] items, string title, bool multiSelect, string[] previewImagePaths, string[] descriptions)
            {
                return new int[1] { 0 };
            }

            public int DialogYesNo(string text, string title)
            {
                return 1;
            }

            public void DisplayImage(string imageFilePath)
            {
                Console.WriteLine(imageFilePath);
            }

            public void DisplayText(string text, string title)
            {
                Console.WriteLine(text);
            }

            public bool ExistsFile(string filePath)
            {
                return true;
            }

            public string[] GetActiveESPNames()
            {
                return new string[1] { "oblivion.esm" };
            }

            public string GetFileFromPath(string path)
            {
                return "";
            }

            public FileVersionInfo GetFileVersion(string filePath)
            {
                return FileVersionInfo.GetVersionInfo(filePath);
            }

            public string InputString(string title, string initialContent)
            {
                return "Hi!";
            }

            public void Message(string text, string title)
            {
                Console.WriteLine(text);
            }

            public void Warn(string message)
            {
                Console.WriteLine(message);
            }
        }

        static void Main(string[] args)
        {
            Framework f = new Framework();
            // the framework has to know where a lot of stuff is located
            // some of these can be left out if you know the installer wont ask for them
            // if the installer does need eg the Oblivion.ini file for INI tweaks than you will
            // get an exception
            string oblivion = @"S:\SteamLibrary\steamapps\common\Oblivion"; // my install location
            f.SetOblivionDirectory(oblivion);
            f.SetDataDirectory(Path.Combine(oblivion,"data"));  
            f.SetOBMMVersion(1, 1, 12); // latest official obmm version use this unless you know what you're doing
            f.SetTempDirectory(@"M:\Projects\omod\testDLL\temp");
            // setting the dll path is mostly used for debugging and if you execute the code from somewhere else
            // better safe than sorry
            f.SetDLLPath(@"M:\Projects\OMOD-Extractor\OMOD-Framework\bin\Release\erri120.OMODFramework.dll");

            // after everything is setup you can go ahead and grap the omod
            OMOD omod = new OMOD(@"M:\Projects\omod\testDLL\DarkUId DarN 16.omod", ref f);

            // you can now do whatever you want with the omod, do look at the documentation for all available functions
            String data = omod.ExtractDataFiles(); //extracts all data files and returns the path to them
            String plugins = omod.ExtractPlugins(); //extracts all plugins and returns the path to them

            // before you can run the script, you have to create a class that implements the interface
            // IScriptRunnerFunctions. In this example I created a class called ScriptFunctions that 
            // implements these functions and uses no real data
            ScriptFunctions a = new ScriptFunctions();

            // running the script requires the use of the ScriptRunner class:
            ScriptRunner sr = new ScriptRunner(ref omod, a);
            sr.ExecuteScript(); // the script will run and do its magic
        }
    }
}
