using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using IronPython.Hosting;
using IronPython.Compiler;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;

namespace OblivionModManager.Scripting
{
    internal static class pythonScriptHandler
    {
        private static ScriptEngine engine = null;
        private static Stream tspOutput = null;
        private static Stream tspError = null;
        private static bool setup = false;

        private static void Initialize()
        {
            engine = Python.CreateEngine();


            ICollection<string> serachPaths = engine.GetSearchPaths();
            serachPaths.Add(Path.GetDirectoryName(Program.CurrentDir));
            Python.CreateModule(engine, "__main__");

            setup = true;
        }

        internal static string CheckSyntax(string code)
        {
            if (!setup) Initialize();
            string errout = "";
            CompiledCode data;
            try
            {
                data = engine.CreateScriptSourceFromString(code).Execute();
            } catch(Exception e)
            {
                errout = e.Message;
            } finally
            {
                data = null;
            }
            return errout;
        }

        public static void Execute(string python_script, IScriptFunctions sf)
        {
            throw new NotImplementedException();
        }
    }
}
