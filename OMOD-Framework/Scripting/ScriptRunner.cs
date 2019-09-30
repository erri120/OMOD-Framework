using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMODFramework.Scripting
{
    public class ScriptRunner
    {
        internal ScriptType type;
        internal string script;
        internal string DataPath;
        internal string PluginsPath;

        public ScriptRunner(OMOD omod)
        {
            script = omod.GetScript();
            if ((byte)script[0] >= (byte)ScriptType.Count) type = ScriptType.obmmScript;
            else
            {
                type = (ScriptType)script[0];
                script = script.Substring(1);
            }
        }

        // bool showWarnings - show warn boxes
        // Action<string> Warn - displays a warn box
        public void ExecuteOBMMScript()
        {

        }

        /// <summary>
        /// Returns the name of the script type
        /// </summary>
        /// <returns>Possible types: obmmScript, Python, cSharp, vb</returns>
        public string GetScriptType() { return Enum.GetName(typeof(ScriptType), type); }
        /// <summary>
        /// Returns the entire script without the first byte
        /// </summary>
        /// <returns></returns>
        public string GetScript() { return script; }
    }
}
