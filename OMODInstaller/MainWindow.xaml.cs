using System;
using System.IO;
using System.Windows;
using CommandLine;
using OMODExtractorDLL;

namespace OMODInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs()).WithParsed<Options>(o =>
            {
                OMOD omod = new OMOD(o.InputFile, o.OutputDir+"//", Path.Combine(o.OutputDir,o.TempDir));
                omod.SaveFile("script");
                InitializeComponent();
            });
        }
    }
}
