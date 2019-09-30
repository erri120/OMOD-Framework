using System;
using System.IO;
using OMODFramework;
using OMODFramework.Scripting;
namespace OMOD_Framework_Example
{
    class Program
    {
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
            f.SetOblivionINIPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Oblivion", "Oblivion.ini"));
            f.SetPluginsListPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Local", "Oblivion", "Plugins.txt"));
            f.SetOBMMVersion(1, 1, 12); // latest official obmm version use this unless you know what you're doing
            f.SetOutputDirectory(@"M:\Projects\omod\testDLL\out");
            f.SetTempDirectory(@"M:\Projects\omod\testDLL\temp");
            // setting the dll path is mostly used for debugging and if you execute the code from somewhere else
            // better safe than sorry
            f.SetDLLPath(@"M:\Projects\OMOD-Extractor\OMOD-Framework\bin\Release\erri120.OMODFramework.dll");

            // after everything is setup you can go ahead and grap the omod
            OMOD omod = new OMOD(@"M:\Projects\omod\testDLL\DarkUId DarN 16.omod", f);

            // you can now do whatever you want with the omod, do look at the documentation for all available functions
            String data = omod.ExtractDataFiles(); //extracts all data files and returns the path to them
            String plugins = omod.ExtractPlugins(); //extracts all plugins and returns the path to them

            // running the script will require the use of the ScriptRunner class:
            ScriptRunner sr = new ScriptRunner(omod);

            // executing the script requires a lot of arguments
            // in this example we don't have any UI and install everything without user input:
            string[] dummyESPList = new string[1] { "oblivion.esm" };
            sr.ExecuteScript(
                (s1) => { Console.WriteLine(s1); },                                     //warning
                (s1, s2) => { return 1; },                                              //dialogYesNo
                (s1) => { return true; },                                               //existsFile
                null,                                                                   // getFileVersion
                (s1, s2, b1, s3, s4) => { return new int[1] { 1 }; },                   //dialogSelect
                (s1, s2) => { Console.WriteLine($"Title: {s2}, msg: {s1}"); },          //message
                null,                                                                   //displayImage
                (s1,s2) => { Console.WriteLine($"Title: {s2}, msg: {s1}"); },           //displayText
                (s1, s2) => { return "Hi"; },                                           //inputString
                () => { return dummyESPList; },                                         //getActiveESPNames
                (s1) => { return Path.Combine(data, s1); }                              //getFileFromPath
                );
            // the script will run and do its magic
        }
    }
}
