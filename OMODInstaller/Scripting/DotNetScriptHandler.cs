using System;
using System.Security.Policy;
using System.CodeDom.Compiler;
using System.Reflection;
using sList = System.Collections.Generic.List<string>;

namespace OblivionModManager.Scripting
{
    internal static class DotNetScriptHandler
    {
        private static readonly Microsoft.CSharp.CSharpCodeProvider csCompiler = new Microsoft.CSharp.CSharpCodeProvider();
        private static readonly Microsoft.VisualBasic.VBCodeProvider vbCompiler = new Microsoft.VisualBasic.VBCodeProvider();
        private static readonly CompilerParameters cParams;
        private static readonly Evidence evidence;

        private static readonly string ScriptOutputPath = Program.TempDir + "dotnetscript.dll";

        static DotNetScriptHandler()
        {
            cParams = new CompilerParameters();
            cParams.GenerateExecutable = false;
            cParams.GenerateInMemory = false;
            cParams.IncludeDebugInformation = false;
            cParams.OutputAssembly = ScriptOutputPath;
            cParams.ReferencedAssemblies.Add(Program.CurrentDir+"OMODInstaller.exe");
            cParams.ReferencedAssemblies.Add("System.dll");
            cParams.ReferencedAssemblies.Add("System.Drawing.dll");
            cParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            cParams.ReferencedAssemblies.Add("System.Xml.dll");

            evidence = new Evidence();
            evidence.AddHostEvidence(new Zone(System.Security.SecurityZone.Internet));
        }

        private static byte[] Compile(string code, ScriptType language)
        {
            return Compile(code, out _, out _, out _, language);
        }

        private static byte[] Compile(string code, out string[] errors, out string[] warnings, out string stdout, ScriptType language)
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
            for (int i = 0; i < results.Output.Count; i++) stdout += results.Output[i] + "\n";
            if (results.Errors.HasErrors)
            {
                sList msgs = new sList();
                foreach (CompilerError ce in results.Errors)
                {
                    if (!ce.IsWarning) msgs.Add($"Error on Line {ce.Line}: {ce.ErrorText}");
                }
                errors = msgs.ToArray();
            }
            else errors = null;
            if (results.Errors.HasWarnings)
            {
                sList msgs = new sList();
                foreach (CompilerError ce in results.Errors)
                {
                    if (ce.IsWarning) msgs.Add($"Warning on Line {ce.Line}: {ce.ErrorText}");
                }
                warnings = msgs.ToArray();
            }
            else warnings = null;
            if (results.Errors.HasErrors)
            {
                return null;
            }
            else
            {
                byte[] data = System.IO.File.ReadAllBytes(results.PathToAssembly);
                //System.IO.File.Delete(results.PathToAssembly);
                return data;
            }
        }

        private static void Execute(string script, IScriptFunctions functions, ScriptType language)
        {
            byte[] data = Compile(script, language);
            if (data == null)
            {
                System.Windows.Forms.MessageBox.Show("C# script failed to compile", "Error");
                return;
            }
            //Assembly asm = AppDomain.CurrentDomain.Load(data, null, evidence);
            Assembly asm = AppDomain.CurrentDomain.Load(data, null);
            IScript s = asm.CreateInstance("Script") as IScript;
            if (s == null)
            {
                System.Windows.Forms.MessageBox.Show("C# or vb script did not contain a 'Script' class in the root namespace, or IScript was not implemented",
                    "Error");
                functions.FatalError();
                return;
            }
            try
            {
                s.Execute(functions);
            }
            catch (ExecutionCancelledException)
            {
                functions.FatalError();
            }
            finally
            {
                Classes.BSAArchive.Clear();
            }
        }

        internal static void ExecuteCS(string script, IScriptFunctions functions)
        {
            Execute(script, functions, ScriptType.cSharp);
        }
    }
}
