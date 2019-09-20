using CommandLine;

namespace OMODInstaller
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "The OMOD file or archive containing the omod file")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "The Output folder")]
        public string OutputDir { get; set; }

        [Option('z', "sevenzip", Required = false, Default = false, HelpText = "Sets the usage of 7zip, only needed if the input is an archive")]
        public bool UseSevenZip { get; set; }

        [Option('t', "temp", Required = false, Default = "temp", HelpText = "The temp folder")]
        public string TempDir { get; set; }
    }
}
