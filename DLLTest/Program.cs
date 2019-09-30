using System;
using System.IO;

namespace DLLTest
{
    class Hello
    {
        internal Action<string> Message;

        public Hello(Action<string> message)
        {
            Message = message;
        }

        public void ShowMessage(string msg)
        {
            Message(msg);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Hello h = new Hello((str) => { Console.WriteLine(str); });
            h.ShowMessage("nice");
            bool b = true;
        }
    }
}
