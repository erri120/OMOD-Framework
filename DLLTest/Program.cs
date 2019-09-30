using System;
using System.IO;
using OMODFramework;
using OMODFramework.Scripting;

namespace DLLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Framework f = new Framework();
            OMOD omod = new OMOD("test",f);
            ScriptRunner sr = new ScriptRunner(omod);
        }
    }
}
