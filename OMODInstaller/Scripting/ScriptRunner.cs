using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OblivionModManager.Scripting
{
    internal static class ScriptRunner
    {
        internal static ScriptReturnData ExecuteScript(string script, string DataPath, string PluginsPath)
        {
            if (script == null || script.Length == 0) return new ScriptReturnData(); ;
            ScriptType type;
            if ((byte)script[0] >= (byte)ScriptType.Count) type = ScriptType.obmmScript;
            else
            {
                type = (ScriptType)script[0];
                script = script.Substring(1);
            }
            if (type == ScriptType.obmmScript)
            {
                //TODO: return obmmScriptHandler.Execute(script, DataPath, PluginsPath);
                return null;
            }

            ScriptReturnData srd = new ScriptReturnData();

            ScriptFunctions sf = new ScriptFunctions(srd, DataPath, PluginsPath);

            switch (type)
            {
                case ScriptType.Python:
                    pythonScriptHandler.Execute(script, sf);
                    break;
                case ScriptType.cSharp:
                    DotNetScriptHandler.ExecuteCS(script, sf);
                    break;
                case ScriptType.vb:
                    DotNetScriptHandler.ExecuteVB(script, sf);
                    break;
            }
            return srd;
        }
    }
}
