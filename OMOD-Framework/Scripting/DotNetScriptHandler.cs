using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Security;
using System.Security.Policy;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using OblivionModManager.Scripting;
using sList = System.Collections.Generic.List<string>;

namespace OMODFramework.Scripting
{
    internal static class DotNetScriptHandler
    {
        private static readonly CSharpCodeProvider csCompiler = new CSharpCodeProvider();
        private static readonly VBCodeProvider vbCompiler = new VBCodeProvider();
        private static readonly CompilerParameters cParams;

        private static readonly string ScriptOutputPath = Path.Combine(Framework.TempDir, "erri120.OMODFramework.dotnetscript.dll");

        static DotNetScriptHandler()
        {
            cParams = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                IncludeDebugInformation = false,
                OutputAssembly = ScriptOutputPath
            };
            cParams.ReferencedAssemblies.Add(Framework.DLLPath);
            cParams.ReferencedAssemblies.Add("System.dll");
            cParams.ReferencedAssemblies.Add("System.IO.dll");
            cParams.ReferencedAssemblies.Add("System.Drawing.dll");
            cParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            cParams.ReferencedAssemblies.Add("System.Xml.dll");

            var evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(SecurityZone.Internet));
        }

        private static byte[] Compile(string code, ScriptType language)
        {
            return Compile(code, out _, out _, out _, language);
        }

        private static byte[] Compile(
            string code, out string[] errors,
            out string[] warnings, out string stdout,
            ScriptType language)
        {
            CompilerResults results;
            switch (language)
            {
                case ScriptType.cSharp:
                    results = csCompiler.CompileAssemblyFromSource(cParams, code);
                    break;
                case ScriptType.vb:
                    results = vbCompiler.CompileAssemblyFromSource(cParams, code);
                    break;
                default:
                    throw new NotImplementedException();
            }
            stdout = "";
            foreach (var t in results.Output)
                stdout += t + "\n";

            if (results.Errors.HasErrors)
            {
                var msgs = new sList();
                foreach (CompilerError ce in results.Errors)
                {
                    if (!ce.IsWarning) msgs.Add($"Error on Line {ce.Line}: {ce.ErrorText}");
                }
                errors = msgs.ToArray();
            }
            else errors = null;
            if (results.Errors.HasWarnings)
            {
                var msgs = new sList();
                foreach (CompilerError ce in results.Errors)
                {
                    if (ce.IsWarning) msgs.Add($"Warning on Line {ce.Line}: {ce.ErrorText}");
                }
                warnings = msgs.ToArray();
            }
            else warnings = null;
            if (results.Errors.HasErrors)
            {
                string e = "", w = "";
                if(warnings != null)
                {
                    foreach (var s in warnings) w += s;
                }

                if (errors != null)
                    foreach (var s in errors)
                        e += s;
                throw new Exception($"Problems during script compilation: \n{e} \n{w}");
            }

            byte[] data = File.ReadAllBytes(results.PathToAssembly);
            //System.IO.File.Delete(results.PathToAssembly);
            return data;
        }

        private static void Execute(
            string script, ref IScriptFunctions functions,
            ScriptType language)
        {
            byte[] data = Compile(script, language);
            if (data == null) throw new Exception("There was an error during script compilation!");
            var asm = AppDomain.CurrentDomain.Load(data, null);
            if (!(asm.CreateInstance("Script") is IScript s))
            {
                throw new Exception("C# or vb script did not contain a 'Script' class in the root namespace, or IScript was not implemented");
            }
            s.Execute(functions);
        }

        internal static void ExecuteCS(string script, IScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.cSharp);
        }

        internal static void ExecuteVB(string script, IScriptFunctions functions)
        {
            Execute(script, ref functions, ScriptType.vb);
        }
    }
}
