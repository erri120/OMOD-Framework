﻿using CommandLine;

namespace OMODExtractor
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "The OMOD file, can also be an archive containing the omod file")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "The Output folder")]
        public string OutputDir { get; set; }

        [Option('z', "sevenzip", Required = false, Default = false, HelpText = "Sets the usage of 7zip, only needed if the input is an archive")]
        public bool UseSevenZip { get; set; }

        [Option('c', "config", Required = false, Default = true, HelpText = "Extract the config to config.txt")]
        public bool ExtractConfig { get; set; }

        [Option('d', "data", Required = false, Default = true, HelpText = "Extract the data.crc to data/")]
        public bool ExtractData { get; set; }

        [Option('p', "plugin", Required = false, Default = true, HelpText = "Extract the plugins.crc to plugins/")]
        public bool ExtractPlugins { get; set; }

        [Option('s', "script", Required = false, Default = false, HelpText = "Extract the script to script.txt")]
        public bool ExtractScript { get; set; }

        [Option('r', "readme", Required = false, Default = false, HelpText = "Extract the readme to readme.txt")]
        public bool ExtractReadme { get; set; }

        [Option('q', "quiet", Required = false, Default = false, HelpText = "Sets the program to silent mode, no console outputs")]
        public bool IsQuiet { get; set; }

        [Option('t',"temp", Required = false, Default = "temp", HelpText = "The temp folder")]
        public string TempDir { get; set; }
    }
}
