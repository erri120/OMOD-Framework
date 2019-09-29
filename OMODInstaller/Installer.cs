using OblivionModManager;
using OMODInstaller.Forms;
using CommandLine;
using System.Windows.Forms;
using System;
using System.IO;

namespace OMODInstaller
{
    internal class Installer
    {

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                // parse the arguments
                Program.CurrentDir = (Path.GetDirectoryName(Application.ExecutablePath) + "\\").ToLower();
                Program.DataDir = o.DataDir+"\\";
                Program.OblivionINIDir = o.INIDir+"\\";
                Program.OblivionESPDir = o.PluginsDir + "\\";
                if (o.TempDir != null) Program.TempDir = o.TempDir+"\\";
                Program.ClearTempFiles();
                if (o.WriteData)
                {
                    Program.UseOutputDir = true;
                    Program.OutputDir = o.OutputDir + "\\";
                }
                if(o.EXEFile != null)
                {
                    Program.UseEXE = true;
                    Program.EXEFile = o.EXEFile;
                }
                // cleanup leftovers
                if (Directory.Exists(Program.OutputDir))
                {
                    DirectoryInfo di = new DirectoryInfo(Program.OutputDir);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        if(!file.Name.Equals("meta.ini")) file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                else
                {
                    Directory.CreateDirectory(Program.OutputDir);
                }

                // read the existing plugins
                Program.ReadPlugins();

                // start out installer
                OMOD omod = new OMOD(o.InputFile);
                omod.InstallOMOD();


                //string s = omod.GetScript();
                //using(StreamWriter sw = new StreamWriter(File.Create(Program.OutputDir + "script")))
                //{
                //    sw.WriteLine(s);
                //}
                //Application.Run(new MainForm());
                //Application.Run(new TextEditor("Test", "None", true, true));

                // cleanup
                Program.ClearTempFiles();
            });
        }
    }
}
