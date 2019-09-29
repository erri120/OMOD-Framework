using OMODFramework;
using System;
using System.IO;

namespace DLLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Framework f = new Framework();
            OMOD omod = new OMOD(args[0],f);
            omod.GetDataFiles();
        }
    }
}
