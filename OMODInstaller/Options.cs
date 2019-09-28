﻿using CommandLine;

namespace OMODInstaller
{
    internal class Options
    {
        [Option('i', "input", Required = true, HelpText = "The OMOD file")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output folder")]
        public string OutputDir { get; set; }

        [Option('w', "write", Default = true, Required = true, HelpText = "Write output files to output folder or game folder?")]
        public bool WriteData { get; set; }

        [Option('d', "data", Required = true, HelpText = "The data folder")]
        public string DataDir { get; set; }

        [Option('p', "plugins", Required = true, HelpText = "Folder containing plugins.txt")]
        public string PluginsDir { get; set; }

        [Option('n', "ini", Required = true, HelpText = "Folder containing Oblivion.ini")]
        public string INIDir { get; set; }

        [Option('t', "temp", Required = false, HelpText = "Temp folder")]
        public string TempDir { get; set; } = null;

        [Option('x', "exe", Required = false, HelpText = "Path to OMODInstaller.exe")]
        public string EXEFile { get; set; } = null;
    }
}
