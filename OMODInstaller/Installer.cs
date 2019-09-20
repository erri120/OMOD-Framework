using System;
using CommandLine;

namespace OMODInstaller
{
    class Installer
    {
        static void Main(String[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {

            });
        }
    }
}
