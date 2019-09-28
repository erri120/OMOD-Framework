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
                Program.CurrentDir = (Path.GetDirectoryName(Application.ExecutablePath) + "\\").ToLower();
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

                OMOD omod = new OMOD(o.InputFile);
                omod.InstallOMOD();
                //Application.Run(new MainForm());
                //Application.Run(new TextEditor("Test", "None", true, true));

                Program.ClearTempFiles();
            });
        }
    }
}
