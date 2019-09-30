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
        internal OMOD OMOD;
        internal ScriptReturnData srd;

        public ScriptRunner(OMOD omod)
        {
            this.OMOD = omod;
            script = omod.GetScript();
            if ((byte)script[0] >= (byte)ScriptType.Count) type = ScriptType.obmmScript;
            else
            {
                type = (ScriptType)script[0];
                script = script.Substring(1);
            }
        }

        /// <summary>
        /// Executes the install script inside the OMOD
        /// </summary>
        /// <param name="warn">
        /// Displays a warning (inputs: string - the warning)
        /// </param>
        /// <param name="dialogYesNo">
        /// Displays a yes-no dialog (inputs: string - text; string - title | output: int - 0=no,1=yes)
        /// </param>
        /// <param name="existsFile">
        /// Checks if a file exists in the oblivion folder (inputs: string - path to the file | output: bool)
        /// </param>
        /// <param name="getFileVersion">
        /// Gets the version of a file (inputs: string - path to the file | output: FileVersionInfo - the version)
        /// </param>
        /// <param name="dialogSelect">
        /// Displays select dialog (inputs: string[] - items to display; string - title; bool - multi(true) or single(false)
        /// select; string[] - paths to preview pictures; string[] description of the items | output: int[] - list of  
        /// indexes of the selected items)
        /// </param>
        /// <param name="message">
        /// Displays a message (inputs: string - text; string - title)
        /// </param>
        /// <param name="displayImage">
        /// Displays and image (inputs: string - path to the image)
        /// </param>
        /// <param name="displayText">
        /// Displays text (inputs: string - title; string - contents)
        /// </param>
        /// <param name="inputString">
        /// Opens a text editor for string input (inputs: string - title, string - initial contents | output:
        /// string - either the user input or null if aborted)
        /// </param>
        public void ExecuteScript(Action<string> warn,
            Func<string, string, int> dialogYesNo,
            Func<string, bool> existsFile,
            Func<string, System.Diagnostics.FileVersionInfo> getFileVersion,
            Func<string[], string, bool, string[], string[], int[]> dialogSelect,
            Action<string, string> message,
            Action<string> displayImage,
            Action<string, string> displayText,
            Func<string, string, string> inputString)
        {
            srd = new ScriptReturnData();

            switch (type)
            {
                case ScriptType.obmmScript:
                    srd = OBMMScriptHandler.Execute(
                        OMOD.GetFramework(), script, DataPath, PluginsPath,
                        warn, dialogYesNo, existsFile, getFileVersion, dialogSelect, message, displayImage,
                        displayText, inputString);
                    break;
                case ScriptType.Python:
                    throw new NotImplementedException();
                case ScriptType.cSharp:
                    throw new NotImplementedException();
                case ScriptType.vb:
                    throw new NotImplementedException();
            }
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
