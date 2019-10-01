using System;
using System.Collections.Generic;
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

        private static void DeleteDirectory(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        static void Main(string[] args)
        {
            // BEFORE you checkout the example:
            // this example is not optimized, it is not intended to be used in production
            // this should be used for understanding how the Framework operates, what it 
            // needs, what you can do with it and what it returns

            Framework f = new Framework(); // basic, do this or go home

            string oblivion = @"S:\SteamLibrary\steamapps\common\Oblivion"; // my install location
            string temp = @"M:\Projects\omod\testDLL\temp";
            string OutputDir = @"M:\Projects\omod\testDLL\output";

            // check if the temp and output dir already exist and delete them if they do
            if (Directory.Exists(temp)) DeleteDirectory(temp);
            Directory.CreateDirectory(temp);
            if (Directory.Exists(OutputDir)) DeleteDirectory(OutputDir);
            Directory.CreateDirectory(OutputDir);

            f.SetOblivionDirectory(oblivion);
            f.SetDataDirectory(Path.Combine(oblivion,"data"));  
            f.SetOBMMVersion(1, 1, 12); // latest official obmm version use this unless you know what you're doing
            f.SetTempDirectory(temp);
            // setting the dll path is mostly used for debugging or if you execute the code from somewhere else
            // better safe than sorry just do this if
            f.SetDLLPath(@"M:\Projects\OMOD-Extractor\OMOD-Framework\bin\Release\erri120.OMODFramework.dll");

            // after everything is setup you can go ahead and grap the omod
            OMOD omod = new OMOD(@"M:\Projects\omod\testDLL\Robert Male Body Replacer v52 OMOD-40532-1.omod", ref f);

            // before you run the install script, extract the data files and plugins from the omod
            // ExtractDataFiles will always return something but ExtractPlugins can return null if there is no
            // plugins.crc in the OMOD
            string dataPath = omod.ExtractDataFiles(); //extracts all data files and returns the path to them
            string pluginsPath = omod.ExtractPlugins(); //extracts all plugins and returns the path to them

            // the interface IScriptRunnerFunctions should be implemented by something that you can pass on 
            // as an argument, in this case I created an internal class called ScriptFunctions that implements
            // all functions from the interface
            ScriptFunctions a = new ScriptFunctions();

            // the script runner can execute the script, return the script and/or script type
            ScriptRunner sr = new ScriptRunner(ref omod, a);

            //to get the type: 
            //ScriptType scriptType = sr.GetScriptType();

            //to get the entire script without the first byte: 
            //String script = sr.GetScript();

            // this will execute the script and return all information about what needs to be installed
            ScriptReturnData srd = sr.ExecuteScript();

            // after the script executed go ahead and do whatever you want with the ScriptReturnData:
            // be sure to check if the installation is canceled or you will run into issues
            if (srd.CancelInstall) Console.WriteLine("Installation canceled");

            // in the following example I will create two lists, one for all data files and one for all
            // plugins that need to be installed
            // this may seem non-intuitive since the ScriptReturnData should return this list
            // the thing is that you have InstallAll, Install, Ignore and Copy operations
            // the install script in the omod decideds what is best for itself

            List<string> InstallPlugins = new List<string>();
            List<string> InstallDataFiles = new List<string>();

            // start by checking if you can install all plugins
            if (srd.InstallAllPlugins)
            {
                // simply get all plugin files from the omod and loop through them
                // the s.Contains is just a safety check
                foreach (string s in omod.GetPluginList()) if (!s.Contains("\\")) InstallPlugins.Add(s);
            }
            // if you can't install everything go and check the list called InstallPlugins
            // this list gets populated when InstallAllPlugins is false
            // the Framework comes with two utility functions that helps in creating the temp list:
            // strArrayContains and strArrayRemove
            foreach(string s in srd.InstallPlugins) { if (!Framework.strArrayContains(InstallPlugins, s)) InstallPlugins.Add(s); }
            // next up is removing all plugins that are set to be ignored:
            foreach (string s in srd.IgnorePlugins) { Framework.strArrayRemove(InstallPlugins, s); }
            // last is going through the CopyPlugins list
            // in case you ask why there is a CopyPlugins list and what is does:
            // (it makes more sense with data files but whatever)
            // if the omod has eg this folder structure:
            //
            // installfiles/
            //              Option1/
            //                      Meshes/
            //                      Textures/
            //              Option2/
            //                      Meshes/
            //                      Textures/
            // this is nice for writing the installation script as you kan keep track of what option
            // has what files
            // Authors than call CopyPlugins/Data and move the files from the options folder to
            // the root folder:
            //
            // meshes/
            // textures/
            // installfiles/
            //              Option1/
            //                      Meshes/
            //                      Textures/
            //              Option2/
            //                      Meshes/
            //                      Textures/
            foreach (ScriptCopyDataFile scd in srd.CopyPlugins)
            {
                // check if the file you want to copy actually exists
                if (!File.Exists(Path.Combine(pluginsPath, scd.CopyFrom))) return;
                else
                {
                    // check if the mod author didnt make a mistake
                    if(scd.CopyFrom != scd.CopyTo)
                    {
                        // unlikely but you never know
                        if (File.Exists(Path.Combine(pluginsPath, scd.CopyTo))) File.Delete(Path.Combine(pluginsPath, scd.CopyTo));
                        File.Copy(Path.Combine(pluginsPath, scd.CopyFrom), Path.Combine(pluginsPath, scd.CopyTo));
                    }
                    // important to add the file to the temp list or else it will not be installed
                    if (!Framework.strArrayContains(InstallPlugins, scd.CopyTo)) InstallPlugins.Add(scd.CopyTo);
                }
            }

            // now do the same for the data files :)
            if (srd.InstallAllData)
            {
                foreach(string s in omod.GetDataFileList()) { InstallDataFiles.Add(s); }
            }
            foreach (string s in srd.InstallData) { if (!Framework.strArrayContains(InstallDataFiles, s)) InstallDataFiles.Add(s); }
            foreach (string s in srd.IgnoreData) { Framework.strArrayRemove(InstallDataFiles, s); }
            foreach (ScriptCopyDataFile scd in srd.CopyDataFiles)
            {
                if (!File.Exists(Path.Combine(dataPath, scd.CopyFrom))) return;
                else
                {
                    if(scd.CopyFrom != scd.CopyTo)
                    {
                        // because data files can be in subdirectories we have to check if the folder actually exists
                        string dirName = Path.GetDirectoryName(Path.Combine(dataPath, scd.CopyTo));
                        if(!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
                        if (File.Exists(Path.Combine(dataPath, scd.CopyTo))) File.Delete(Path.Combine(dataPath, scd.CopyTo));
                        File.Copy(Path.Combine(dataPath, scd.CopyFrom), Path.Combine(dataPath, scd.CopyTo));
                    }
                    if (!Framework.strArrayContains(InstallDataFiles, scd.CopyTo)) InstallDataFiles.Add(scd.CopyTo);
                }
            }

            // after everything is done some final checks
            for(int i = 0; i < InstallDataFiles.Count; i++)
            {
                // if the files have \\ at the start than Path.Combine wont work :(
                if (InstallDataFiles[i].StartsWith("\\")) InstallDataFiles[i] = InstallDataFiles[i].Substring(1);
                string currentFile = Path.Combine(dataPath, InstallDataFiles[i]);
                // also check if the file we want to install exists and is not in the 5th dimension eating lunch
                if (!File.Exists(currentFile)) InstallDataFiles.RemoveAt(i--);
            }

            for(int i = 0; i < InstallPlugins.Count; i++)
            {
                if (InstallPlugins[i].StartsWith("\\")) InstallPlugins[i] = InstallPlugins[i].Substring(1);
                string currentFile = Path.Combine(pluginsPath, InstallPlugins[i]);
                if (!File.Exists(currentFile)) InstallPlugins.RemoveAt(i--);
            }

            // now install
            for (int i = 0; i < InstallDataFiles.Count; i++)
            {
                // check if the folder exists before copying
                string s = Path.GetDirectoryName(InstallDataFiles[i]);
                if (!Directory.Exists(Path.Combine(OutputDir, s))) Directory.CreateDirectory(Path.Combine(OutputDir, s));
                File.Move(Path.Combine(dataPath, InstallDataFiles[i]), Path.Combine(OutputDir, InstallDataFiles[i]));
            }
            for(int i = 0; i < InstallPlugins.Count; i++)
            {
                File.Move(Path.Combine(pluginsPath, InstallPlugins[i]), Path.Combine(OutputDir, InstallPlugins[i]));
            }
        }
    }
}
