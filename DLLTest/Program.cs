using OMODFramework;
using System;
using System.IO;

namespace DLLTest
{
    class Program
    {
        enum MyEnum
        {
            TEST,
            NIGGA
        }

        static void Main(string[] args)
        {
            //Framework f = new Framework();
            //OMOD omod = new OMOD(args[0],f);
            //omod.GetDataFiles();

            string a = Enum.GetName(typeof(MyEnum), MyEnum.TEST);
            _ = a.Length;
        }
    }
}
