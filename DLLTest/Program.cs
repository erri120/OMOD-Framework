using OMODFramework;
using System;
using System.IO;

namespace DLLTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string z = "out\\";
            string a = args[0];
            string b = Path.GetFullPath(a);
            string c = args[1];
            string d = Path.Combine(z, c);
            string result = Console.ReadLine();
        }
    }
}
