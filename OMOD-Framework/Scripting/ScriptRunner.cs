using System;
using OblivionModManager.Scripting;

namespace OMODFramework.Scripting
{
    public interface IScriptRunnerFunctions
    {
        /// <summary>
        /// Warns the user about something
        /// </summary>
        /// <param name="message">The warning</param>
        void Warn(string message);
        /// <summary>
        /// Creates a Yes-No dialog. If the user pressed No than return 0,
        /// if the user pressed Yes than return 1
        /// </summary>
        /// <param name="text">Text in the dialog</param>
        /// <param name="title">Title of the dialog</param>
        /// <returns>No: returns 0, Yes: returns 1</returns>
        int DialogYesNo(string text, string title);
        /// <summary>
        /// Checks if a file relative to the oblivion game folder exists
        /// </summary>
        /// <param name="filePath">Relative path based from the oblivion game folder,
        /// eg: 'data//meshes//something//nice.nif'</param>
        /// <returns>True if the file exists, false if it doesn't</returns>
        bool ExistsFile(string filePath);
        /// <summary>
        /// Returns the version of a file relative to the oblivion game folder
        /// </summary>
        /// <param name="filePath">Relative path based from the oblivion game folder,
        /// eg: 'Oblivion.exe', 'obse_loader.exe', 'data//obse//plugins//obge//obge.dll'</param>
        /// <returns>Returns the Version of a file</returns>
        Version GetFileVersion(string filePath);
        /// <summary>
        /// Opens a select Dialog with multiple choices, can either be single or multi select.
        /// Returns an array containing the indexes of the selected items
        /// </summary>
        /// <param name="items">The items to be choosen from</param>
        /// <param name="title">The title of the Dialog</param>
        /// <param name="multiSelect">True for selecting multiple items, False for selecting one item</param>
        /// <param name="previewImagePaths">Absolute paths to the images of the items, when clicking an item 
        /// load the image and display it somewhere in the dialog. If this is empty than no images are available and 
        /// you only need to display descriptions</param>
        /// <param name="descriptions">Descriptions of the items, same as previewImagePaths: when the 
        /// user selects an items display the description somewhere.</param>
        /// <returns>Returns an array containing the indexes of the selected items</returns>
        int[] DialogSelect(string[] items, string title, bool multiSelect, string[] previewImagePaths, string[] descriptions);
        /// <summary>
        /// Shows the user a message
        /// </summary>
        /// <param name="text">Text of the message</param>
        /// <param name="title">Title of the message Dialog</param>
        void Message(string text, string title);
        /// <summary>
        /// Displays an image
        /// </summary>
        /// <param name="imageFilePath">Absolute path of the image</param>
        void DisplayImage(string imageFilePath);
        /// <summary>
        /// Displays text
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="title">Title of the Dialog</param>
        /// <param name="rtf">If the text is RTF</param>
        void DisplayText(string text, string title, bool rtf);
        /// <summary>
        /// Opens a Dialog for inputing text
        /// </summary>
        /// <param name="title">Title of the Dialog</param>
        /// <param name="initialContent">Initial content, can be empty</param>
        /// <returns>The user input text</returns>
        string InputString(string title, string initialContent);
        /// <summary>
        /// Returns a list with the names of all active esps (names mean no extension like esp or esm)
        /// </summary>
        /// <returns>Returns a list with the names of all active esps</returns>
        string[] GetActiveESPNames();
        /// <summary>
        /// Returns a list with names of all existing esps (names mean no extension)
        /// </summary>
        /// <returns>Returns a list with names of all existing esps</returns>
        string[] GetExistingESPNames();
        /// <summary>
        /// Returns the absolute path of a file, do note that this is used for getting the paths
        /// that should be in the oblivion folder but arent (looking at you MO2). The path argument
        /// will give you a relative path based from the oblivion game folder. The file will not 
        /// be changed but read
        /// </summary>
        /// <param name="path">Relative path of the file based from the oblivion game folder</param>
        /// <returns>Returns the absolute path of the requested file</returns>
        string GetFileFromPath(string path);
    }

    public class ScriptRunner
    {
        internal ScriptType type;
        internal string script;
        internal string DataPath;
        internal string PluginsPath;
        internal OMOD OMOD;
        internal IScriptRunnerFunctions ScriptRunnerFunctions;
        internal ScriptReturnData srd;

        /// <summary>
        /// The ScriptRunner is responsible for running a script inside an OMOD
        /// </summary>
        /// <param name="omod">The OMOD with the script to be executed</param>
        /// <param name="scriptRunnerFunctions">All callback functions for execution</param>
        public ScriptRunner(ref OMOD omod, IScriptRunnerFunctions scriptRunnerFunctions)
        {
            OMOD = omod;
            ScriptRunnerFunctions = scriptRunnerFunctions;
            script = omod.GetScript();
            if ((byte)script[0] >= (byte)ScriptType.Count) type = ScriptType.obmmScript;
            else
            {
                type = (ScriptType)script[0];
                script = script.Substring(1);
            }

            DataPath = omod.ExtractDataFiles();
            PluginsPath = omod.ExtractPlugins();
        }

        /// <summary>
        /// Executes the script
        /// </summary>
        public ScriptReturnData ExecuteScript()
        {
            srd = new ScriptReturnData();

            var sf = new ScriptFunctions(
                srd, DataPath, PluginsPath, OMOD.GetFramework(), ScriptRunnerFunctions);

            switch (type)
            {
                case ScriptType.obmmScript:
                    return OBMMScriptHandler.Execute(
                        OMOD.GetFramework(), script, DataPath, PluginsPath, ScriptRunnerFunctions);
                case ScriptType.Python:
                    throw new NotImplementedException();
                case ScriptType.cSharp:
                    DotNetScriptHandler.ExecuteCS(script, sf);
                    break;
                case ScriptType.vb:
                    DotNetScriptHandler.ExecuteVB(script, sf);
                    break;
            }

            return srd;
        }

        /// <summary>
        /// Returns the name of the script type
        /// </summary>
        /// <returns>Possible types: obmmScript, Python, cSharp, vb</returns>
        public ScriptType GetScriptType() { return type; }
        /// <summary>
        /// Returns the entire script without the first byte
        /// </summary>
        /// <returns></returns>
        public string GetScript() { return script; }
    }
}
