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
        static void Main(String[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
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

                OMOD omod = new OMOD(o.InputFile);
                omod.InstallOMOD();

                //Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);
                //Application.Run(new MainForm());
                //Application.Run(new TextEditor("Test", "None", true, true));

                Program.ClearTempFiles();
            });
        }
    }
}
