using System;
using System.IO;
using System.Collections.Generic;
namespace OMODFramework.Scripting
{
    internal static class OBMMScriptHandler
    {
        private class FlowControlStruct
        {
            public readonly int line;
            public readonly byte type;
            public readonly string[] values;
            public readonly string var;
            public bool active;
            public bool hitCase = false;
            public int forCount = 0;

            //Inactive
            public FlowControlStruct(byte type)
            {
                line = -1;
                this.type = type;
                values = null;
                var = null;
                active = false;
            }

            //If
            public FlowControlStruct(int line, bool active)
            {
                this.line = line;
                type = 0;
                values = null;
                var = null;
                this.active = active;
            }

            //Select
            public FlowControlStruct(int line, string[] values)
            {
                this.line = line;
                type = 1;
                this.values = values;
                var = null;
                active = false;
            }

            //For
            public FlowControlStruct(string[] values, string var, int line)
            {
                this.line = line;
                type = 2;
                this.values = values;
                this.var = var;
                active = false;
            }
        }

        private static bool ShowWarnings;
        private static ScriptReturnData srd;
        private static Dictionary<string, string> variables;

        private static string DataFiles;
        private static string Plugins;
        private static string cLine = "0";

        private static Framework f;

        private static string MakeValidFolderPath(string s)
        {
            s = s.Replace('/', '\\');
            if (!s.EndsWith("\\")) s += "\\";
            return s;
        }

        private static string[] SplitLine(string s)
        {
            List<string> temp = new List<string>();
            bool WasLastSpace = false;
            bool InQuotes = false;
            bool WasLastEscape = false;
            bool DoubleBreak = false;
            bool InVar = false;
            string CurrentWord = "";
            string CurrentVar = "";

            if (s == "") return new string[0];
            s += " ";
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '%':
                        WasLastSpace = false;
                        if (InVar)
                        {
                            if (variables.ContainsKey(CurrentWord)) CurrentWord = CurrentVar + variables[CurrentWord];
                            else CurrentWord = CurrentVar + "%" + CurrentWord + "%";
                            CurrentVar = "";
                            InVar = false;
                        }
                        else
                        {
                            if (InQuotes && WasLastEscape)
                            {
                                CurrentWord += "%";
                            }
                            else
                            {
                                InVar = true;
                                CurrentVar = CurrentWord;
                                CurrentWord = "";
                            }
                        }
                        WasLastEscape = false;
                        break;
                    case ',':
                    case ' ':
                        WasLastEscape = false;
                        if (InVar)
                        {
                            CurrentWord = CurrentVar + "%" + CurrentWord;
                            CurrentVar = "";
                            InVar = false;
                        }
                        if (InQuotes)
                        {
                            CurrentWord += s[i];
                        }
                        else if (!WasLastSpace)
                        {
                            if (InVar)
                            {
                                if (!variables.ContainsKey(CurrentWord)) temp.Add("");
                                else temp.Add(variables[CurrentWord]);
                                InVar = false;
                            }
                            else temp.Add(CurrentWord);
                            CurrentWord = "";
                            WasLastSpace = true;
                        }
                        break;
                    case ';':
                        WasLastEscape = false;
                        if (!InQuotes)
                        {
                            DoubleBreak = true;
                        }
                        else CurrentWord += s[i];
                        break;
                    case '"':
                        if (InQuotes && WasLastEscape)
                        {
                            CurrentWord += s[i];
                        }
                        else
                        {
                            if (InVar) Warn("String marker found in the middle of a variable name");
                            if (InQuotes)
                            {
                                InQuotes = false;
                            }
                            else
                            {
                                InQuotes = true;
                            }
                        }
                        WasLastSpace = false;
                        WasLastEscape = false;
                        break;
                    case '\\':
                        if (InQuotes && WasLastEscape)
                        {
                            CurrentWord += s[i];
                            WasLastEscape = false;
                        }
                        else if (InQuotes)
                        {
                            WasLastEscape = true;
                        }
                        else
                        {
                            CurrentWord += s[i];
                        }
                        WasLastSpace = false;
                        break;
                    default:
                        WasLastEscape = false;
                        WasLastSpace = false;
                        CurrentWord += s[i];
                        break;
                }
                if (DoubleBreak) break;
            }
            if (InVar) Warn("Unterminated variable");
            if (InQuotes) Warn("Unterminated quote");
            return temp.ToArray();
        }

        /// <summary>
        /// Creates a popup warn message
        /// string: message
        /// </summary>
        private static Action<string> Warn;
        /// <summary>
        /// Creates a yes-no dialog
        /// string: message
        /// string: title
        /// int: return value (0: no, 1: yes)
        /// </summary>
        private static Func<string, string, int> DialogYesNo;
        /// <summary>
        /// Checks if a file exists in the main oblivion folder
        /// string: relative path of the file
        /// bool: return value (false: doesnt exist, true: exists)
        /// </summary>
        private static Func<string, bool> ExistsFile;
        /// <summary>
        /// Returns the version of a file
        /// string: relative path of the file
        /// FileVersionInfo: return value
        /// </summary>
        private static Func<string, System.Diagnostics.FileVersionInfo> GetFileVersion;
        /// <summary>
        /// Creates a select dialog
        /// string[]: list of all items to be displayed
        /// string: the title of the dialog
        /// bool: multi or single select
        /// string[]: paths to preview pictures
        /// string[]: descriptions of the items
        /// int[]: return value | the index of the selected items
        /// </summary>
        private static Func<string[], string, bool, string[], string[], int[]> DialogSelect;
        /// <summary>
        /// Displays a message in a window
        /// string: input value the text
        /// string: input value the title (can be null if no title given)
        /// </summary>
        private static Action<string, string> Message;
        /// <summary>
        /// Displays an image
        /// string: absolute path to the image
        /// </summary>
        private static Action<string> DisplayImage;
        /// <summary>
        /// Displays text
        /// string: title
        /// string: initial contents
        /// </summary>
        private static Action<string, string> DisplayText;
        /// <summary>
        /// Opens a text editor to input a string
        /// string: title
        /// string: inital contents
        /// string: return value either the user input or null if operation got aborted
        /// </summary>
        private static Func<string, string, string> InputString;


        internal static ScriptReturnData Execute(Framework f, string InputScript, string DataPath, string PluginsPath,
            bool showWarnings, Action<string> warn, Func<string, string, int> dialogYesNo,
            Func<string, bool> existsFile, Func<string, System.Diagnostics.FileVersionInfo> getFileVersion, 
            Func<string[], string, bool, string[], string[], int[]> dialogSelect,
            Action<string, string> message,
            Action<string> displayImage,
            Func<string, string, string> inputString)
        {
            ShowWarnings = showWarnings;
            Warn = warn;
            DialogYesNo = dialogYesNo;
            ExistsFile = existsFile;
            GetFileVersion = getFileVersion;
            DialogSelect = dialogSelect;
            Message = message;
            DisplayImage = displayImage;
            InputString = inputString;

            srd = new ScriptReturnData();
            if (InputScript == null) return srd;

            DataFiles = DataPath;
            Plugins = PluginsPath;
            variables = new Dictionary<string, string>();

            Stack<FlowControlStruct> FlowControl = new Stack<FlowControlStruct>();
            Queue<string> ExtraLines = new Queue<string>();

            variables["NewLine"] = Environment.NewLine;
            variables["Tab"] = "\t";

            string[] script = InputScript.Replace("\r", "").Split('\n');
            string[] line;
            string s;
            bool AllowRunOnLines = false;
            string SkipTo = null;

            for (int i = 0; i < script.Length || ExtraLines.Count > 0; i++)
            {
                if (ExtraLines.Count > 0)
                {
                    i--;
                    s = ExtraLines.Dequeue().Replace('\t', ' ').Trim();
                }
                else
                {
                    s = script[i].Replace('\t', ' ').Trim();
                }
                cLine = i.ToString();
                if (AllowRunOnLines)
                {
                    while (s.EndsWith("\\"))
                    {
                        s = s.Remove(s.Length - 1);
                        if (ExtraLines.Count > 0) s += ExtraLines.Dequeue().Replace('\t', ' ').Trim();
                        else
                        {
                            if (++i == script.Length) Warn("Run-on line passed the end of the script");
                            else s += script[i].Replace('\t', ' ').Trim();
                        }
                    }
                }
                if (SkipTo != null)
                {
                    if (s == SkipTo) SkipTo = null;
                    else continue;
                }
                line = SplitLine(s);
                if (line.Length == 0) continue;

                if (FlowControl.Count != 0 && !FlowControl.Peek().active)
                {
                    //switch the type of action
                    switch (line[0])
                    {
                        case "":
                            Warn("Empty function");
                            break;
                        case "If":
                        case "IfNot":
                            FlowControl.Push(new FlowControlStruct(0));
                            break;
                        case "Else":
                            // checks if the else statement has an if statement or if its just flying around
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 0) FlowControl.Peek().active = FlowControl.Peek().line != -1;
                            else Warn("Unexpected Else statement");
                            break;
                        case "EndIf":
                            // same as else
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 0) FlowControl.Pop();
                            else Warn("Unexpected EndIf statement");
                            break;
                        case "Select":
                        case "SelectMany":
                        case "SelectWithPreview":
                        case "SelectManyWithPreview":
                        case "SelectWithDescriptions":
                        case "SelectManyWithDescriptions":
                        case "SelectWithDescriptionsAndPreviews":
                        case "SelectManyWithDescriptionsAndPreviews":
                        case "SelectVar":
                        case "SelectString":
                            FlowControl.Push(new FlowControlStruct(1));
                            break;
                        case "Case":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 1)
                            {
                                if (FlowControl.Peek().line != -1 && Array.IndexOf(FlowControl.Peek().values, s) != -1)
                                {
                                    FlowControl.Peek().active = true;
                                    FlowControl.Peek().hitCase = true;
                                }
                            }
                            else Warn("Unexpected Break statement");
                            break;
                        case "Default":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 1)
                            {
                                if (FlowControl.Peek().line != -1 && !FlowControl.Peek().hitCase) FlowControl.Peek().active = true;
                            }
                            else Warn("Unexpected Default statement");
                            break;
                        case "EndSelect":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 1) FlowControl.Pop();
                            else Warn("Unexpected EndSelect statement");
                            break;
                        case "For":
                            FlowControl.Push(new FlowControlStruct(2));
                            break;
                        case "EndFor":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 2) FlowControl.Pop();
                            else Warn("Unexpected EndFor statement");
                            break;
                        case "Break":
                        case "Continue":
                        case "Exit":
                            break;
                    }
                }
                else
                {
                    switch (line[0])
                    {
                        case "":
                            Warn("Empty function");
                            break;
                        case "Goto":
                            if (line.Length < 2)
                            {
                                Warn("Not enough arguments to function 'Goto'");
                            }
                            else
                            {
                                if (line.Length > 2) Warn("Unexpected extra arguments to function 'Goto'");
                                SkipTo = "Label " + line[1];
                                FlowControl.Clear();
                            }
                            break;
                        case "Label":
                            break;
                        case "If":
                            FlowControl.Push(new FlowControlStruct(i, FunctionIf(line)));
                            break;
                        case "IfNot":
                            FlowControl.Push(new FlowControlStruct(i, !FunctionIf(line)));
                            break;
                        case "Else":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 0) FlowControl.Peek().active = false;
                            else Warn("Unexpected Else");
                            break;
                        case "EndIf":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 0) FlowControl.Pop();
                            else Warn("Unexpected EndIf");
                            break;
                        case "Select":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, false)));
                            break;
                        case "SelectMany":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, false)));
                            break;
                        case "SelectWithPreview":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, false)));
                            break;
                        case "SelectManyWithPreview":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, false)));
                            break;
                        case "SelectWithDescriptions":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, false, true)));
                            break;
                        case "SelectManyWithDescriptions":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, false, true)));
                            break;
                        case "SelectWithDescriptionsAndPreviews":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, false, true, true)));
                            break;
                        case "SelectManyWithDescriptionsAndPreviews":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelect(line, true, true, true)));
                            break;
                        case "SelectVar":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, true)));
                            break;
                        case "SelectString":
                            FlowControl.Push(new FlowControlStruct(i, FunctionSelectVar(line, false)));
                            break;
                        case "Break":
                            {
                                bool found = false;
                                FlowControlStruct[] fcs = FlowControl.ToArray();
                                for (int k = 0; k < fcs.Length; k++)
                                {
                                    if (fcs[k].type == 1)
                                    {
                                        for (int j = 0; j <= k; j++) fcs[j].active = false;
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) Warn("Unexpected Break");
                                break;
                            }
                        case "Case":
                            if (FlowControl.Count == 0 || FlowControl.Peek().type != 1) Warn("Unexpected Case");
                            break;
                        case "Default":
                            if (FlowControl.Count == 0 || FlowControl.Peek().type != 1) Warn("Unexpected Default");
                            break;
                        case "EndSelect":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 1) FlowControl.Pop();
                            else Warn("Unexpected EndSelect");
                            break;
                        case "For":
                            {
                                FlowControlStruct fc = FunctionFor(line, i);
                                FlowControl.Push(fc);
                                if (fc.line != -1 && fc.values.Length > 0)
                                {
                                    variables[fc.var] = fc.values[0];
                                    fc.active = true;
                                }
                                break;
                            }
                        case "Continue":
                            {
                                bool found = false;
                                FlowControlStruct[] fcs = FlowControl.ToArray();
                                for (int k = 0; k < fcs.Length; k++)
                                {
                                    if (fcs[k].type == 2)
                                    {
                                        fcs[k].forCount++;
                                        if (fcs[k].forCount == fcs[k].values.Length)
                                        {
                                            for (int j = 0; j <= k; j++) fcs[j].active = false;
                                        }
                                        else
                                        {
                                            i = fcs[k].line;
                                            variables[fcs[k].var] = fcs[k].values[fcs[k].forCount];
                                            for (int j = 0; j < k; j++) FlowControl.Pop();
                                        }
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) Warn("Unexpected Continue");
                                break;
                            }
                        case "Exit":
                            {
                                bool found = false;
                                FlowControlStruct[] fcs = FlowControl.ToArray();
                                for (int k = 0; k < fcs.Length; k++)
                                {
                                    if (fcs[k].type == 2)
                                    {
                                        for (int j = 0; j <= k; j++) FlowControl.Peek().active = false;
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) Warn("Unexpected Exit");
                                break;
                            }
                        case "EndFor":
                            if (FlowControl.Count != 0 && FlowControl.Peek().type == 2)
                            {
                                FlowControlStruct fc = FlowControl.Peek();
                                fc.forCount++;
                                if (fc.forCount == fc.values.Length) FlowControl.Pop();
                                else
                                {
                                    i = fc.line;
                                    variables[fc.var] = fc.values[fc.forCount];
                                }
                            }
                            else Warn("Unexpected EndFor");
                            break;
                        //Functions
                        case "Message":
                            FunctionMessage(line);
                            break;
                        case "LoadEarly":
                            FunctionLoadEarly(line);
                            break;
                        case "LoadBefore":
                            FunctionLoadOrder(line, false);
                            break;
                        case "LoadAfter":
                            FunctionLoadOrder(line, true);
                            break;
                        case "ConflictsWith":
                            FunctionConflicts(line, true, false);
                            break;
                        case "DependsOn":
                            FunctionConflicts(line, false, false);
                            break;
                        case "ConflictsWithRegex":
                            FunctionConflicts(line, true, true);
                            break;
                        case "DependsOnRegex":
                            FunctionConflicts(line, false, true);
                            break;
                        case "DontInstallAnyPlugins":
                            srd.InstallAllPlugins = false;
                            break;
                        case "DontInstallAnyDataFiles":
                            srd.InstallAllData = false;
                            break;
                        case "InstallAllPlugins":
                            srd.InstallAllPlugins = true;
                            break;
                        case "InstallAllDataFiles":
                            srd.InstallAllData = true;
                            break;
                        case "InstallPlugin":
                            FunctionModifyInstall(line, true, true);
                            break;
                        case "DontInstallPlugin":
                            FunctionModifyInstall(line, true, false);
                            break;
                        case "InstallDataFile":
                            FunctionModifyInstall(line, false, true);
                            break;
                        case "DontInstallDataFile":
                            FunctionModifyInstall(line, false, false);
                            break;
                        case "DontInstallDataFolder":
                            FunctionModifyInstallFolder(line, false);
                            break;
                        case "InstallDataFolder":
                            FunctionModifyInstallFolder(line, true);
                            break;
                        case "RegisterBSA":
                            FunctionRegisterBSA(line, true);
                            break;
                        case "UnregisterBSA":
                            FunctionRegisterBSA(line, false);
                            break;
                        case "FatalError":
                            srd.CancelInstall = true;
                            break;
                        case "Return":
                            Break = true;
                            break;
                        case "UncheckESP":
                            FunctionUncheckESP(line);
                            break;
                        case "SetDeactivationWarning":
                            FunctionSetDeactivationWarning(line);
                            break;
                        case "CopyDataFile":
                            FunctionCopyDataFile(line, false);
                            break;
                        case "CopyPlugin":
                            FunctionCopyDataFile(line, true);
                            break;
                        case "CopyDataFolder":
                            FunctionCopyDataFolder(line);
                            break;
                        case "PatchPlugin":
                            FunctionPatch(line, true);
                            break;
                        case "PatchDataFile":
                            FunctionPatch(line, false);
                            break;
                        case "EditINI":
                            FunctionEditINI(line);
                            break;
                        case "EditSDP":
                        case "EditShader":
                            FunctionEditShader(line);
                            break;
                        case "SetGMST":
                            FunctionSetEspVar(line, true);
                            break;
                        case "SetGlobal":
                            FunctionSetEspVar(line, false);
                            break;
                        case "SetPluginByte":
                            FunctionSetEspData(line, typeof(byte));
                            break;
                        case "SetPluginShort":
                            FunctionSetEspData(line, typeof(short));
                            break;
                        case "SetPluginInt":
                            FunctionSetEspData(line, typeof(int));
                            break;
                        case "SetPluginLong":
                            FunctionSetEspData(line, typeof(long));
                            break;
                        case "SetPluginFloat":
                            FunctionSetEspData(line, typeof(float));
                            break;
                        case "DisplayImage":
                            FunctionDisplayFile(line, true);
                            break;
                        case "DisplayText":
                            FunctionDisplayFile(line, false);
                            break;
                        case "SetVar":
                            FunctionSetVar(line);
                            break;
                        case "GetFolderName":
                        case "GetDirectoryName":
                            FunctionGetDirectoryName(line);
                            break;
                        case "GetFileName":
                            FunctionGetFileName(line);
                            break;
                        case "GetFileNameWithoutExtension":
                            FunctionGetFileNameWithoutExtension(line);
                            break;
                        case "CombinePaths":
                            FunctionCombinePaths(line);
                            break;
                        case "Substring":
                            FunctionSubstring(line);
                            break;
                        case "RemoveString":
                            FunctionRemoveString(line);
                            break;
                        case "StringLength":
                            FunctionStringLength(line);
                            break;
                        case "InputString":
                            FunctionInputString(line);
                            break;
                        case "ReadINI":
                            FunctionReadINI(line);
                            break;
                        case "ReadRendererInfo":
                            FunctionReadRenderer(line);
                            break;
                        case "ExecLines":
                            FunctionExecLines(line, ExtraLines);
                            break;
                        case "iSet":
                            FunctionSet(line, true);
                            break;
                        case "fSet":
                            FunctionSet(line, false);
                            break;
                        case "EditXMLLine":
                            FunctionEditXMLLine(line);
                            break;
                        case "EditXMLReplace":
                            FunctionEditXMLReplace(line);
                            break;
                        case "AllowRunOnLines":
                            AllowRunOnLines = true;
                            break;
                        default:
                            Warn("Unknown function '" + line[0] + "'");
                            break;
                    }
                }
            }
            if (SkipTo != null) Warn($"Expected {SkipTo}!");
            ScriptReturnData TempResult = srd;
            srd = null;
            variables = null;

            return TempResult;
        }

        #region Functions

        private static bool FunctionIf(string[] line)
        {
            if (line.Length == 1)
            {
                Warn("Missing arguments for IF statement!");
                return false;
            }
            switch (line[1])
            {
                case "DialogYesNo":
                    switch (line.Length)
                    {
                        case 2:
                            Warn("Missing arguments to function 'If DialogYesNo'");
                            return false;
                        case 3:
                            return DialogYesNo(line[2], "") == 1;
                        case 4:
                            return DialogYesNo(line[2], line[3]) == 1;
                        default:
                            Warn("Unexpected arguments after function 'If DialogYesNo'");
                            goto case 4;
                    }
                case "DataFileExists":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If DataFileExists'");
                        return false;
                    }
                    return ExistsFile(Path.Combine("data",line[2]));
                case "VersionGreaterThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If VersionGreaterThan'");
                        return false;
                    }
                    try
                    {
                        Version v = new Version(line[2] + ".0");
                        Version v2 = new Version($"{f.OBMMFakeMajorVersion.ToString()}.{f.OBMMFakeMinorVersion.ToString()}.{f.OBMMFakeBuildNumber.ToString()}.0");
                        return (v2 > v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If VersionGreaterThan'");
                        return false;
                    }
                case "VersionLessThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If VersionLessThan'");
                        return false;
                    }
                    try
                    {
                        Version v = new Version(line[2] + ".0");
                        Version v2 = new Version($"{f.OBMMFakeMajorVersion.ToString()}.{f.OBMMFakeMinorVersion.ToString()}.{f.OBMMFakeBuildNumber.ToString()}.0");
                        return (v2 < v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If VersionLessThan'");
                        return false;
                    }
                case "ScriptExtenderPresent":
                    if (line.Length > 2) Warn("Unexpected arguments to 'If ScriptExtenderPresent'");
                    return ExistsFile("obse_loader.exe");
                case "ScriptExtenderNewerThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If ScriptExtenderNewerThan'");
                        return false;
                    }
                    if (line.Length > 3) Warn("Unexpected arguments to 'If ScriptExtenderNewerThan'");
                    if (ExistsFile("obse_loader.exe")) return false;
                    try
                    {
                        System.Diagnostics.FileVersionInfo fvi = GetFileVersion("obse_loader.exe");
                        if (fvi.FileVersion == null) return false;
                        Version v = new Version(line[2]); ;
                        Version v2 = new Version(fvi.FileVersion.Replace(", ", "."));
                        return (v2 >= v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If ScriptExtenderNewerThan'");
                        return false;
                    }
                case "GraphicsExtenderPresent":
                    if (line.Length > 2) Warn("Unexpected arguments to 'If GraphicsExtenderPresent'");
                    return ExistsFile(Path.Combine("data","obse","plugins","obge.dll"));
                case "GraphicsExtenderNewerThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If GraphicsExtenderNewerThan'");
                        return false;
                    }
                    if (line.Length > 3) Warn("Unexpected arguments to 'If GraphicsExtenderNewerThan'");
                    if (ExistsFile(Path.Combine("data", "obse", "plugins", "obge.dll"))) return false;
                    try
                    {
                        System.Diagnostics.FileVersionInfo fvi = GetFileVersion(Path.Combine("data", "obse", "plugins", "obge.dll"));
                        if (fvi.FileVersion == null) return false;
                        Version v = new Version(line[2]); ;
                        Version v2 = new Version(fvi.FileVersion.Replace(", ", "."));
                        return (v2 >= v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If GraphicsExtenderNewerThan'");
                        return false;
                    }
                case "OblivionNewerThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If OblivionNewerThan'");
                        return false;
                    }
                    if (line.Length > 3) Warn("Unexpected arguments to 'If OblivionNewerThan'");
                    try
                    {
                        System.Diagnostics.FileVersionInfo fvi = GetFileVersion("oblivion.exe");
                        if (fvi.FileVersion == null) return false;
                        Version v = new Version(line[2]); ;
                        Version v2 = new Version(fvi.FileVersion.Replace(", ", "."));
                        bool b = v2 >= v;
                        return (v2 >= v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If OblivionNewerThan'");
                        return false;
                    }
                case "Equal":
                    if (line.Length < 4)
                    {
                        Warn("Missing arguments to function 'If Equal'");
                        return false;
                    }
                    if (line.Length > 4) Warn("Unexpected arguments to 'If Equal'");
                    return line[2] == line[3];
                case "GreaterEqual":
                case "GreaterThan":
                    {
                        if (line.Length < 4)
                        {
                            Warn("Missing arguments to function 'If Greater'");
                            return false;
                        }
                        if (line.Length > 4) Warn("Unexpected arguments to 'If Greater'");
                        if (!int.TryParse(line[2], out int arg1) || !int.TryParse(line[3], out int arg2))
                        {
                            Warn("Invalid argument upplied to function 'If Greater'");
                            return false;
                        }
                        if (line[1] == "GreaterEqual") return arg1 >= arg2;
                        else return arg1 > arg2;
                    }
                case "fGreaterEqual":
                case "fGreaterThan":
                    {
                        if (line.Length < 4)
                        {
                            Warn("Missing arguments to function 'If fGreater'");
                            return false;
                        }
                        if (line.Length > 4) Warn("Unexpected arguments to 'If fGreater'");
                        if (!double.TryParse(line[2], out double arg1) || !double.TryParse(line[3], out double arg2))
                        {
                            Warn("Invalid argument upplied to function 'If fGreater'");
                            return false;
                        }
                        if (line[1] == "fGreaterEqual") return arg1 >= arg2;
                        else return arg1 > arg2;
                    }
                default:
                    Warn("Unknown argument '" + line[1] + "' supplied to 'If'");
                    return false;
            }

        }

        private static string[] FunctionSelect(string[] line, bool many, bool previews, bool descriptions)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to function 'Select'");
                return new string[0];
            }

            string[] Items, Previews, Descs;
            int argsPerOption = 1 + (previews ? 1 : 0) + (descriptions ? 1 : 0);

            string title = line[1];
            Items = new string[line.Length - 2];
            Array.Copy(line, 2, Items, 0, line.Length - 2);
            line = Items;

            if (line.Length % argsPerOption != 0)
            {
                Warn("Unexpected extra arguments to 'Select'");
                Array.Resize(ref line, line.Length - line.Length % argsPerOption);
            }

            Items = new string[line.Length / argsPerOption];
            Previews = previews ? new string[line.Length / argsPerOption] : null;
            Descs = descriptions ? new string[line.Length / argsPerOption] : null;

            for (int i = 0; i < line.Length / argsPerOption; i++)
            {
                Items[i] = line[i * argsPerOption];
                if (previews)
                {
                    Previews[i] = line[i * argsPerOption + 1];
                    if (descriptions) Descs[i] = line[i * argsPerOption + 2];
                }
                else
                {
                    if (descriptions) Descs[i] = line[i * argsPerOption + 1];
                }
            }

            if (Previews != null)
            {
                for (int i = 0; i < Previews.Length; i++)
                {
                    if (Previews[i] == "None") Previews[i] = null;
                    else if (!Framework.IsSafeFileName(Previews[i]))
                    {
                        Warn($"Preview file path '{Previews[i]}' was invalid");
                        Previews[i] = null;
                    }
                    else if (!File.Exists(Path.Combine(DataFiles, Previews[i])))
                    {
                        Warn($"Preview file path '{Previews[i]}' does not exist");
                        Previews[i] = null;
                    }
                    else
                    {
                        Previews[i] = Path.Combine(DataFiles, Previews[i]);
                    }
                }
            }
            int[] dialogResult = DialogSelect(Items, title, many, Previews, Descs);
            string[] result = new string[dialogResult.Length];
            for(int i = 0; i < dialogResult.Length; i++)
            {
                result[i] = $"Case {Items[dialogResult[i]]}";
            }
            return result;
        }

        private static string[] FunctionSelectVar(string[] line, bool IsVariable)
        {
            string Func;
            if (IsVariable) Func = " to function 'SelectVar'"; else Func = "to function 'SelectString'";
            if (line.Length < 2)
            {
                Warn($"Missing arguments{Func}");
                return new string[0];
            }
            if (line.Length > 2) Warn($"Unexpected arguments{Func}");
            if (IsVariable)
            {
                if (!variables.ContainsKey(line[1]))
                {
                    Warn($"Invalid argument{Func}\nVariable '{line[1]}' does not exist");
                    return new string[0];
                }
                else return new string[] { "Case " + variables[line[1]] };
            }
            else
            {
                return new string[] { "Case " + line[1] };
            }
        }

        private static FlowControlStruct FunctionFor(string[] line, int LineNo)
        {
            FlowControlStruct NullLoop = new FlowControlStruct(2);
            if (line.Length < 3)
            {
                Warn("Missing arguments to function 'For'");
                return NullLoop;
            }

            if (line[1] == "Each") line[1] = line[2];
            switch (line[1])
            {
                case "Count":
                    {
                        if (line.Length < 5)
                        {
                            Warn("Missing arguments to function 'For Count'");
                            return NullLoop;
                        }
                        if (line.Length > 6) Warn("Unexpected extra arguments to 'For Count'");
                        int step = 1;
                        if (!int.TryParse(line[3], out int start) || !int.TryParse(line[4], out int end) || (line.Length >= 6 && !int.TryParse(line[5], out step)))
                        {
                            Warn("Invalid argument to 'For Count'");
                            return NullLoop;
                        }
                        List<string> steps = new List<string>();
                        for (int i = start; i <= end; i += step)
                        {
                            steps.Add(i.ToString());
                        }
                        return new FlowControlStruct(steps.ToArray(), line[2], LineNo);
                    }
                case "DataFolder":
                    {
                        if (line.Length < 5)
                        {
                            Warn("Missing arguments to function 'For Each DataFolder'");
                            return NullLoop;
                        }
                        if (line.Length > 7) Warn("Unexpected extra arguments to 'For Each DataFolder'");
                        if (!Framework.IsSafeFolderName(line[4]))
                        {
                            Warn($"Invalid argument to 'For Each DataFolder'\nDirectory '{line[4]}' is not valid");
                            return NullLoop;
                        }
                        if (!Directory.Exists(Path.Combine(DataFiles, line[4])))
                        {
                            Warn($"Invalid argument to 'For Each DataFolder'\nDirectory '{line[4]}' is not a part of this plugin");
                            return NullLoop;
                        }
                        SearchOption option = SearchOption.TopDirectoryOnly;
                        if (line.Length > 5)
                        {
                            switch (line[5])
                            {
                                case "True":
                                    option = SearchOption.AllDirectories;
                                    break;
                                case "False":
                                    break;
                                default:
                                    Warn($"Invalid argument '{line[5]}' to 'For Each DataFolder'.\nExpected 'True' or 'False'");
                                    break;
                            }
                        }
                        try
                        {
                            string[] paths = Directory.GetDirectories(Path.Combine(DataFiles, line[4]), line.Length > 6 ? line[6] : "*", option);
                            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(DataFiles.Length);
                            return new FlowControlStruct(paths, line[3], LineNo);
                        }
                        catch
                        {
                            Warn("Invalid argument to 'For Each DataFolder'");
                            return NullLoop;
                        }
                    }
                case "PluginFolder":
                    {
                        if (line.Length < 5)
                        {
                            Warn("Missing arguments to function 'For Each PluginFolder'");
                            return NullLoop;
                        }
                        if (line.Length > 7) Warn("Unexpected extra arguments to 'For Each PluginFolder'");
                        if (!Framework.IsSafeFolderName(line[4]))
                        {
                            Warn($"Invalid argument to 'For Each PluginFolder'\nDirectory '{line[4]}' is not valid");
                            return NullLoop;
                        }
                        if (!Directory.Exists(Path.Combine(Plugins, line[4])))
                        {
                            Warn($"Invalid argument to 'For Each PluginFolder'\nDirectory '{line[4]}' is not a part of this plugin");
                            return NullLoop;
                        }
                        SearchOption option = SearchOption.TopDirectoryOnly;
                        if (line.Length > 5)
                        {
                            switch (line[5])
                            {
                                case "True":
                                    option = SearchOption.AllDirectories;
                                    break;
                                case "False":
                                    break;
                                default:
                                    Warn($"Invalid argument '{line[5]}' to 'For Each PluginFolder'.\nExpected 'True' or 'False'");
                                    break;
                            }
                        }
                        try
                        {
                            string[] paths = Directory.GetDirectories(Path.Combine(Plugins, line[4]), line.Length > 6 ? line[6] : "*", option);
                            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(Plugins.Length);
                            return new FlowControlStruct(paths, line[3], LineNo);
                        }
                        catch
                        {
                            Warn("Invalid argument to 'For Each PluginFolder'");
                            return NullLoop;
                        }
                    }
                case "DataFile":
                    {
                        if (line.Length < 5)
                        {
                            Warn("Missing arguments to function 'For Each DataFile'");
                            return NullLoop;
                        }
                        if (line.Length > 7) Warn("Unexpected extra arguments to 'For Each DataFile'");
                        if (!Framework.IsSafeFolderName(line[4]))
                        {
                            Warn($"Invalid argument to 'For Each DataFile'\nDirectory '{line[4]}' is not valid");
                            return NullLoop;
                        }
                        if (!Directory.Exists(Path.Combine(DataFiles, line[4])))
                        {
                            Warn($"Invalid argument to 'For Each DataFile'\nDirectory '{line[4]}' is not a part of this plugin");
                            return NullLoop;
                        }
                        SearchOption option = SearchOption.TopDirectoryOnly;
                        if (line.Length > 5)
                        {
                            switch (line[5])
                            {
                                case "True":
                                    option = SearchOption.AllDirectories;
                                    break;
                                case "False":
                                    break;
                                default:
                                    Warn($"Invalid argument '{line[5]}' to 'For Each DataFile'.\nExpected 'True' or 'False'");
                                    break;
                            }
                        }
                        try
                        {
                            string[] paths = Directory.GetFiles(Path.Combine(DataFiles, line[4]), line.Length > 6 ? line[6] : "*", option);
                            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(DataFiles.Length);
                            return new FlowControlStruct(paths, line[3], LineNo);
                        }
                        catch
                        {
                            Warn("Invalid argument to 'For Each DataFile'");
                            return NullLoop;
                        }
                    }
                case "Plugin":
                    {
                        if (line.Length < 5)
                        {
                            Warn("Missing arguments to function 'For Each Plugin'");
                            return NullLoop;
                        }
                        if (line.Length > 7) Warn("Unexpected extra arguments to 'For Each Plugin'");
                        if (!Framework.IsSafeFolderName(line[4]))
                        {
                            Warn($"Invalid argument to 'For Each Plugin'\nDirectory '{line[4]}' is not valid");
                            return NullLoop;
                        }
                        if (!Directory.Exists(Path.Combine(Plugins, line[4])))
                        {
                            Warn($"Invalid argument to 'For Each Plugin'\nDirectory '{line[4]}' is not a part of this plugin");
                            return NullLoop;
                        }
                        SearchOption option = SearchOption.TopDirectoryOnly;
                        if (line.Length > 5)
                        {
                            switch (line[5])
                            {
                                case "True":
                                    option = SearchOption.AllDirectories;
                                    break;
                                case "False":
                                    break;
                                default:
                                    Warn($"Invalid argument '{line[5]}' to 'For Each Plugin'.\nExpected 'True' or 'False'");
                                    break;
                            }
                        }
                        try
                        {
                            string[] paths = Directory.GetFiles(Path.Combine(Plugins, line[4]), line.Length > 6 ? line[6] : "*", option);
                            for (int i = 0; i < paths.Length; i++) if (Path.IsPathRooted(paths[i])) paths[i] = paths[i].Substring(Plugins.Length);
                            return new FlowControlStruct(paths, line[3], LineNo);
                        }
                        catch
                        {
                            Warn("Invalid argument to 'For Each Plugin'");
                            return NullLoop;
                        }
                    }
            }
            return NullLoop;
        }

        private static void FunctionMessage(string[] line)
        {
            switch (line.Length)
            {
                case 1:
                    Warn("Missing arguments to function 'Message'");
                    break;
                case 2:
                    Message(line[1], null);
                    break;
                case 3:
                    Message(line[1], line[2]);
                    break;
                default:
                    Message(line[1], line[2]);
                    Warn("Unexpected arguments after 'Message'");
                    break;
            }
        }

        private enum LoadOrderTypes { AFTER, BEFORE, EARLY };

        private static void FunctionLoadEarly(string[] line)
        {
            if (line.Length < 2)
            {
                Warn("Missing arguments to LoadEarly");
                return;
            }
            else if (line.Length > 2)
            {
                Warn("Unexpected arguments to LoadEarly");
            }
            line[1] = line[1].ToLower();
            //if (!srd.EarlyPlugins.Contains(line[1])) srd.EarlyPlugins.Add(line[1]);
            CreateLoadOrderAdvise(line[1], null, LoadOrderTypes.EARLY);
        }

        private static void FunctionLoadOrder(string[] line, bool LoadAfter)
        {
            string WarnMess;
            if (LoadAfter) WarnMess = "function 'LoadAfter'"; else WarnMess = "function 'LoadBefore'";
            if (line.Length < 3)
            {
                Warn("Missing arguments to " + WarnMess);
                return;
            }
            else if (line.Length > 3)
            {
                Warn("Unexpected arguments to " + WarnMess);
            }
            //srd.LoadOrderList.Add(new PluginLoadInfo(line[1], line[2], LoadAfter));
            CreateLoadOrderAdvise(line[1], line[2], LoadAfter ? LoadOrderTypes.AFTER : LoadOrderTypes.BEFORE);
        }

        private static void CreateLoadOrderAdvise(string plugin1, string plugin2, LoadOrderTypes type)
        {
            string adviseFile = Path.Combine(Framework.OutputDir, "loadorder_advise.txt");
            List<string> contents = new List<string>();
            if (File.Exists(adviseFile))
            {
                using (StreamReader sr = new StreamReader(File.OpenRead(adviseFile), System.Text.Encoding.Default))
                {
                    while (sr.Peek() != -1)
                    {
                        contents.Add(sr.ReadLine());
                    }
                }
                File.Delete(adviseFile);
            }
            switch (type)
            {
                case LoadOrderTypes.AFTER:
                    contents.Add($"Place {plugin1} after {plugin2}");
                    break;
                case LoadOrderTypes.BEFORE:
                    contents.Add($"Place {plugin1} before {plugin2}");
                    break;
                case LoadOrderTypes.EARLY:
                    contents.Add($"Place {plugin1} early in your load order");
                    break;
            }
            using (StreamWriter sw = new StreamWriter(File.Create(adviseFile), System.Text.Encoding.Default))
            {
                foreach (string s in contents)
                {
                    sw.WriteLine(s);
                }
            }
        }

        private static void FunctionConflicts(string[] line, bool Conflicts, bool Regex)
        {
            string WarnMess;
            if (Conflicts) WarnMess = "function 'ConflictsWith"; else WarnMess = "function 'DependsOn";
            if (Regex) WarnMess += "Regex'"; else WarnMess += "'";
            ConflictData cd = new ConflictData
            {
                level = ConflictLevel.MajorConflict
            };
            switch (line.Length)
            {
                case 1:
                    Warn($"Missing arguments to {WarnMess}");
                    return;
                case 2:
                    cd.File = line[1];
                    break;
                case 3:
                    cd.Comment = line[2];
                    goto case 2;
                case 4:
                    switch (line[3])
                    {
                        case "Unusable":
                            cd.level = ConflictLevel.Unusable;
                            break;
                        case "Major":
                            cd.level = ConflictLevel.MajorConflict;
                            break;
                        case "Minor":
                            cd.level = ConflictLevel.MinorConflict;
                            break;
                        default:
                            Warn($"Unknown conflict level after {WarnMess}");
                            break;
                    }
                    goto case 3;
                case 5:
                    Warn($"Unexpected arguments to {WarnMess}");
                    goto case 4;
                case 6:
                    cd.File = line[1];
                    try
                    {
                        cd.MinMajorVersion = Convert.ToInt32(line[2]);
                        cd.MinMinorVersion = Convert.ToInt32(line[3]);
                        cd.MaxMajorVersion = Convert.ToInt32(line[4]);
                        cd.MaxMinorVersion = Convert.ToInt32(line[5]);
                    }
                    catch
                    {
                        Warn($"Arguments to {WarnMess} in incorrect format");
                        return;
                    }
                    break;
                case 7:
                    cd.Comment = line[6];
                    goto case 6;
                case 8:
                    switch (line[7])
                    {
                        case "Unusable":
                            cd.level = ConflictLevel.Unusable;
                            break;
                        case "Major":
                            cd.level = ConflictLevel.MajorConflict;
                            break;
                        case "Minor":
                            cd.level = ConflictLevel.MinorConflict;
                            break;
                        default:
                            Warn($"Unknown conflict level after {WarnMess}");
                            break;
                    }
                    goto case 7;
                default:
                    Warn($"Unexpected arguments to {WarnMess}");
                    goto case 8;
            }
            cd.Partial = Regex;
            if (Conflicts) srd.ConflictsWith.Add(cd); else srd.DependsOn.Add(cd);
        }

        private static void FunctionModifyInstall(string[] line, bool plugins, bool Install)
        {
            string WarnMess;
            if (plugins) { if (Install) WarnMess = "function 'InstallPlugin'"; else WarnMess = "function 'DontInstallPlugin'"; }
            else { if (Install) WarnMess = "function 'InstallDataFile'"; else WarnMess = "function 'DontInstallDataFile'"; }
            if (line.Length == 1)
            {
                Warn($"Missing arguments to {WarnMess}");
                return;
            }
            if (line.Length > 2) Warn($"Unexpected arguments after {WarnMess}");
            if (plugins)
            {
                if (!File.Exists(Path.Combine(Plugins, line[1])))
                {
                    Warn($"Invalid argument to {WarnMess}\nFile '{line[1]}' is not part of this plugin");
                    return;
                }
                if (line[1].IndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) != -1)
                {
                    Warn($"Invalid argument to {WarnMess}\nThis function cannot be used on plugins stored in subdirectories");
                    return;
                }
                if (Install)
                {
                    Framework.strArrayRemove(srd.IgnorePlugins, line[1]);
                    if (!Framework.strArrayContains(srd.InstallPlugins, line[1])) srd.InstallPlugins.Add(line[1]);
                }
                else
                {
                    Framework.strArrayRemove(srd.InstallPlugins, line[1]);
                    if (!Framework.strArrayContains(srd.IgnorePlugins, line[1])) srd.IgnorePlugins.Add(line[1]);
                }
            }
            else
            {
                if (!File.Exists(Path.Combine(DataFiles + line[1])))
                {
                    Warn($"Invalid argument to {WarnMess}\nFile '{line[1]}' is not part of this plugin");
                    return;
                }
                if (Install)
                {
                    Framework.strArrayRemove(srd.IgnoreData, line[1]);
                    if (!Framework.strArrayContains(srd.InstallData, line[1])) srd.InstallData.Add(line[1]);
                }
                else
                {
                    Framework.strArrayRemove(srd.InstallData, line[1]);
                    if (!Framework.strArrayContains(srd.IgnoreData, line[1])) srd.IgnoreData.Add(line[1]);
                }
            }
        }

        private static void FunctionModifyInstallFolder(string[] line, bool Install)
        {
            string WarnMess;
            if (Install) WarnMess = "function 'InstallDataFolder'"; else WarnMess = "function 'DontInstallDataFolder'";
            if (line.Length == 1)
            {
                Warn($"Missing arguments to {WarnMess}");
                return;
            }
            if (line.Length > 3) Warn($"Unexpected arguments to {WarnMess}");
            line[1] = MakeValidFolderPath(line[1]);

            if (!Directory.Exists(Path.Combine(DataFiles, line[1])))
            {
                Warn($"Invalid argument to {WarnMess}\nFolder '{line[1]} ' is not part of this plugin");
                return;
            }

            if (line.Length >= 3)
            {
                switch (line[2])
                {
                    case "True":
                        foreach (string folder in Directory.GetDirectories(Path.Combine(DataFiles, line[1])))
                        {
                            FunctionModifyInstallFolder(new string[] { "", folder.Substring(DataFiles.Length), "True" }, Install);
                        }
                        break;
                    case "False":
                        break;
                    default:
                        Warn($"Invalid argument to {WarnMess}\nExpected True or False");
                        return;
                }
            }

            foreach (string path in Directory.GetFiles(Path.Combine(DataFiles, line[1])))
            {
                string file = line[1] + Path.GetFileName(path);
                if (Install)
                {
                    Framework.strArrayRemove(srd.IgnoreData, file);
                    if (!Framework.strArrayContains(srd.InstallData, file)) srd.InstallData.Add(file);
                }
                else
                {
                    Framework.strArrayRemove(srd.InstallData, file);
                    if (!Framework.strArrayContains(srd.IgnoreData, file)) srd.IgnoreData.Add(file);
                }
            }
        }

        private static void FunctionRegisterBSA(string[] line, bool Register) { }

        private static void FunctionUncheckESP(string[] line) { }

        private static void FunctionSetDeactivationWarning(string[] line) { }

        private static void FunctionCopyDataFile(string[] line, bool Plugin)
        {
            string WarnMess;
            if (Plugin) WarnMess = "function 'CopyPlugin'"; else WarnMess = "function 'CopyDataFile'";
            if (line.Length < 3)
            {
                Warn($"Missing arguments to {WarnMess}");
                return;
            }
            if (line.Length > 3) Warn($"Unexpected arguments to {WarnMess}");
            string upperfrom = line[1];
            string upperto = line[2];
            line[1] = line[1].ToLower();
            line[2] = line[2].ToLower();
            if (!Framework.IsSafeFileName(line[1]) || !Framework.IsSafeFileName(line[2]))
            {
                Warn($"Invalid argument to {WarnMess}");
                return;
            }
            if (line[1] == line[2])
            {
                Warn($"Invalid argument to {WarnMess}\nYou cannot copy a file over itself");
                return;
            }
            if (Plugin)
            {
                if (!File.Exists(Path.Combine(Plugins, line[1])))
                {
                    Warn($"Invalid argument to CopyPlugin\nFile '{upperfrom}' is not part of this plugin");
                    return;
                }
                if (line[2].IndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) != -1)
                {
                    Warn("Plugins cannot be copied to subdirectories of the data folder");
                    return;
                }
                if (!(line[2].EndsWith(".esp") || line[2].EndsWith(".esm")))
                {
                    Warn("Copied plugins must have a .esp or .esm extension");
                    return;
                }
            }
            else
            {
                if (!File.Exists(Path.Combine(DataFiles, line[1])))
                {
                    Warn($"Invalid argument to CopyDataFile\nFile '{upperfrom}' is not part of this plugin");
                    return;
                }
                if (line[2].EndsWith(".esp") || line[2].EndsWith(".esm"))
                {
                    Warn("Copied data files cannot have a .esp or .esm extension");
                    return;
                }
            }

            if (Plugin)
            {
                for (int i = 0; i < srd.CopyPlugins.Count; i++)
                {
                    if (srd.CopyPlugins[i].CopyTo == line[2]) srd.CopyPlugins.RemoveAt(i--);
                }
                srd.CopyPlugins.Add(new ScriptCopyDataFile(upperfrom, upperto));
            }
            else
            {
                for (int i = 0; i < srd.CopyDataFiles.Count; i++)
                {
                    if (srd.CopyDataFiles[i].CopyTo == line[2]) srd.CopyDataFiles.RemoveAt(i--);
                }
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(upperfrom, upperto));
            }
        }

        private static void FunctionCopyDataFolder(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to CopyDataFolder");
                return;
            }
            if (line.Length > 4) Warn("Unexpected arguments to CopyDataFolder");
            line[1] = MakeValidFolderPath(line[1].ToLower());
            line[2] = MakeValidFolderPath(line[2].ToLower());
            if (!Framework.IsSafeFolderName(line[1]) || !Framework.IsSafeFolderName(line[2]))
            {
                Warn("Invalid argument to CopyDataFolder");
                return;
            }
            if (!Directory.Exists(Path.Combine(DataFiles, line[1])))
            {
                Warn($"Invalid argument to CopyDataFolder\nFolder '{line[1]}' is not part of this plugin");
                return;
            }
            if (line[1] == line[2])
            {
                Warn("Invalid argument to CopyDataFolder\nYou cannot copy a folder over itself");
                return;
            }

            if (line.Length >= 4)
            {
                switch (line[3])
                {
                    case "True":
                        foreach (string folder in Directory.GetDirectories(Path.Combine(DataFiles, line[1])))
                        {
                            FunctionCopyDataFolder(new string[] { "", folder.Substring(DataFiles.Length), line[2] + folder.Substring(DataFiles.Length + line[1].Length), "True" });
                        }
                        break;
                    case "False":
                        break;
                    default:
                        Warn("Invalid argument to CopyDataFolder\nExpected True or False");
                        return;
                }
            }

            foreach (string s in Directory.GetFiles(Path.Combine(DataFiles, line[1])))
            {
                string from = line[1] + Path.GetFileName(s);
                string to = line[2] + Path.GetFileName(s);
                string lto = to.ToLower();
                for (int i = 0; i < srd.CopyDataFiles.Count; i++)
                {
                    if (srd.CopyDataFiles[i].CopyTo == lto) srd.CopyDataFiles.RemoveAt(i--);
                }
                srd.CopyDataFiles.Add(new ScriptCopyDataFile(from, to));
            }
        }

        private static void FunctionPatch(string[] line, bool Plugin)
        {
            string WarnMess;
            if (Plugin) WarnMess = "function 'PatchPlugin'"; else WarnMess = "function 'PatchDataFile'";
            if (line.Length < 3)
            {
                Warn($"Missing arguments to {WarnMess}");
                return;
            }
            if (line.Length > 4) Warn($"Unexpected arguments to {WarnMess}");
            string lowerTo = line[2].ToLower();
            if (!Framework.IsSafeFileName(line[1]) || !Framework.IsSafeFileName(line[2]))
            {
                Warn($"Invalid argument to {WarnMess}");
                return;
            }
            string copypath;
            if (Plugin)
            {
                copypath = Path.Combine(Plugins, line[1]);
                if (!File.Exists(copypath))
                {
                    Warn($"Invalid argument to PatchPlugin\nFile '{line[1]}' is not part of this plugin");
                    return;
                }
                if (line[2].IndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) != -1)
                {
                    Warn("Plugins cannot be copied to subdirectories of the data folder");
                    return;
                }
                if (!(lowerTo.EndsWith(".esp") || lowerTo.EndsWith(".esm")))
                {
                    Warn("Plugins must have a .esp or .esm extension");
                    return;
                }

            }
            else
            {
                copypath = Path.Combine(DataFiles, line[1]);
                if (!File.Exists(copypath))
                {
                    Warn($"Invalid argument to PatchDataFile\nFile '{line[1]}' is not part of this plugin");
                    return;
                }
                if (lowerTo.EndsWith(".esp") || lowerTo.EndsWith(".esm"))
                {
                    Warn("Data files cannot have a .esp or .esm extension");
                    return;
                }
            }
            if (line.Length < 4 || line[3] != "True") return;
            //TODO: check if this actually works
            File.Move(copypath, Path.Combine(Framework.OutputDir, line[2]));
        }

        private static void FunctionEditINI(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to EditINI");
                return;
            }
            if (line.Length > 4) Warn("Unexpected arguments to EditINI");
            srd.INIEdits.Add(new INIEditInfo(line[1], line[2], line[3]));
        }

        private static void FunctionEditShader(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to 'EditShader'");
                return;
            }
            if (line.Length > 4) Warn("Unexpected arguments to 'EditShader'");
            if (!Framework.IsSafeFileName(line[3]))
            {
                Warn($"Invalid argument to 'EditShader'\n'{ line[3]}' is not a valid file name");
                return;
            }
            if (!File.Exists(Path.Combine(DataFiles, line[3])))
            {
                Warn($"Invalid argument to 'EditShader'\nFile '{line[3]}' does not exist");
                return;
            }
            if (!byte.TryParse(line[1], out byte package))
            {
                Warn($"Invalid argument to function 'EditShader'\n'{line[1]}' is not a valid shader package ID");
                return;
            }
            srd.SDPEdits.Add(new SDPEditInfo(package, line[2], Path.Combine(DataFiles, line[3])));
        }

        private static void FunctionSetEspVar(string[] line, bool GMST) { }

        private static void FunctionSetEspData(string[] line, Type type)
        {
            string WarnMess = null;
            if (type == typeof(byte)) WarnMess = "function 'SetPluginByte'";
            else if (type == typeof(short)) WarnMess = "function 'SetPluginShort'";
            else if (type == typeof(int)) WarnMess = "function 'SetPluginInt'";
            else if (type == typeof(long)) WarnMess = "function 'SetPluginLong'";
            else if (type == typeof(float)) WarnMess = "function 'SetPluginFloat'";
            if (line.Length < 4)
            {
                Warn($"Missing arguments to {WarnMess}");
                return;
            }
            if (line.Length > 4) Warn($"Unexpected extra arguments to {WarnMess}");
            if (!Framework.IsSafeFileName(line[1]))
            {
                Warn($"Illegal plugin name supplied to {WarnMess}");
                return;
            }
            if (!File.Exists(Path.Combine(Plugins, line[1])))
            {
                Warn($"Invalid argument to {WarnMess}\nFile '{line[1]}' is not part of this plugin");
                return;
            }
            byte[] data = null;
            if (!long.TryParse(line[2], out long offset) || offset < 0)
            {
                Warn($"Invalid argument to {WarnMess}\nOffset '{line[1]}' is not valid");
                return;
            }
            if (type == typeof(byte))
            {
                if (!byte.TryParse(line[3], out byte value))
                {
                    Warn($"Invalid argument to {WarnMess}\nValue '{line[3]}' is not valid");
                    return;
                }
                data = BitConverter.GetBytes(value);
            }
            if (type == typeof(short))
            {
                if (!short.TryParse(line[3], out short value))
                {
                    Warn($"Invalid argument to {WarnMess}\nValue '{line[3]}' is not valid");
                    return;
                }
                data = BitConverter.GetBytes(value);
            }
            if (type == typeof(int))
            {
                if (!int.TryParse(line[3], out int value))
                {
                    Warn($"Invalid argument to {WarnMess}\nValue '{line[3]}' is not valid");
                    return;
                }
                data = BitConverter.GetBytes(value);
            }
            if (type == typeof(long))
            {
                if (!long.TryParse(line[3], out long value))
                {
                    Warn($"Invalid argument to {WarnMess}\nValue '{line[3]}' is not valid");
                    return;
                }
                data = BitConverter.GetBytes(value);
            }
            if (type == typeof(float))
            {
                if (!float.TryParse(line[3], out float value))
                {
                    Warn($"Invalid argument to {WarnMess}\nValue '{line[3]}' is not valid");
                    return;
                }
                data = BitConverter.GetBytes(value);
            }
            using (FileStream fs = File.OpenWrite(Path.Combine(Plugins, line[1])))
            {
                if (offset + data.Length >= fs.Length)
                {
                    Warn($"Invalid argument to {WarnMess}\nOffset '{line[3]}' is out of range");
                    fs.Close();
                    return;
                }
                fs.Position = offset;
                fs.Write(data, 0, data.Length);
            }
        }

        private static void FunctionDisplayFile(string[] line, bool Image)
        {
            string WarnMess;
            if (Image) WarnMess = "function 'DisplayImage'"; else WarnMess = "function 'DisplayText'";
            if (line.Length < 2)
            {
                Warn("Missing arguments to " + WarnMess);
                return;
            }
            if (line.Length > 3) Warn("Unexpected extra arguments to " + WarnMess);
            if (!Framework.IsSafeFileName(line[1]))
            {
                Warn("Illegal path supplied to " + WarnMess);
                return;
            }
            if (!File.Exists(DataFiles + line[1]))
            {
                Warn($"Non-existant file '{line[1]}' supplied to " + WarnMess);
                return;
            }
            if (Image)
            {
                DisplayImage(Path.Combine(DataFiles, line[1]));
            }
            else
            {
                string s = File.ReadAllText(Path.Combine(DataFiles + line[1]), System.Text.Encoding.Default);
                DisplayText((line.Length > 2) ? line[2] : line[1], s);
            }
        }

        private static void FunctionSetVar(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to function SetVar");
                return;
            }
            if (line.Length > 3) Warn("Unexpected extra arguments to function SetVar");
            variables[line[1]] = line[2];
        }

        private static void FunctionGetDirectoryName(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to GetDirectoryName");
                return;
            }
            if (line.Length > 3) Warn("Unexpected arguments to GetDirectoryName");
            try
            {
                variables[line[1]] = Path.GetDirectoryName(line[2]);
            }
            catch
            {
                Warn("Invalid argument to GetDirectoryName");
            }
        }

        private static void FunctionGetFileName(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to GetFileName");
                return;
            }
            if (line.Length > 3) Warn("Unexpected arguments to GetFileName");
            try
            {
                variables[line[1]] = Path.GetFileName(line[2]);
            }
            catch
            {
                Warn("Invalid argument to GetFileName");
            }
        }

        private static void FunctionGetFileNameWithoutExtension(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to GetFileNameWithoutExtension");
                return;
            }
            if (line.Length > 3) Warn("Unexpected arguments to GetFileNameWithoutExtension");
            try
            {
                variables[line[1]] = Path.GetFileNameWithoutExtension(line[2]);
            }
            catch
            {
                Warn("Invalid argument to GetFileNameWithoutExtension");
            }
        }

        private static void FunctionCombinePaths(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to CombinePaths");
                return;
            }
            if (line.Length > 4) Warn("Unexpected arguments to CombinePaths");
            try
            {
                variables[line[1]] = Path.Combine(line[2], line[3]);
            }
            catch
            {
                Warn("Invalid argument to CombinePaths");
            }
        }

        private static void FunctionSubstring(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to Substring");
                return;
            }
            if (line.Length > 5) Warn("Unexpected extra arguments to Substring");
            if (line.Length == 4)
            {
                if (!int.TryParse(line[3], out int start))
                {
                    Warn("Invalid argument to Substring");
                    return;
                }
                variables[line[1]] = line[2].Substring(start);
            }
            else
            {
                if (!int.TryParse(line[3], out int start) || !int.TryParse(line[4], out int end))
                {
                    Warn("Invalid argument to Substring");
                    return;
                }
                variables[line[1]] = line[2].Substring(start, end);
            }
        }

        private static void FunctionRemoveString(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to RemoveString");
                return;
            }
            if (line.Length > 5) Warn("Unexpected extra arguments to RemoveString");
            if (line.Length == 4)
            {
                if (!int.TryParse(line[3], out int start))
                {
                    Warn("Invalid argument to RemoveString");
                    return;
                }
                variables[line[1]] = line[2].Remove(start);
            }
            else
            {
                if (!int.TryParse(line[3], out int start) || !int.TryParse(line[4], out int end))
                {
                    Warn("Invalid argument to RemoveString");
                    return;
                }
                variables[line[1]] = line[2].Remove(start, end);
            }
        }

        private static void FunctionStringLength(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to StringLength");
                return;
            }
            if (line.Length > 3) Warn("Unexpected extra arguments to StringLength");
            variables[line[1]] = line[2].Length.ToString();
        }

        private static void FunctionInputString(string[] line)
        {
            if (line.Length < 2)
            {
                Warn("Missing arguments to InputString");
                return;
            }
            if (line.Length > 4) Warn("Unexpected arguments to InputString");

            string title = "";
            string initial = "";

            if (line.Length > 2) title = line[2];
            if (line.Length > 3) initial = line[3];

            string result = InputString(title, initial);
            if (result == null) variables[line[1]] = "";
            else variables[line[1]] = result;
        }

        private static void FunctionReadINI(string[] line)
        {
            if (line.Length < 4)
            {
                Warn("Missing arguments to function ReadINI");
                return;
            }
            if (line.Length > 4) Warn("Unexpected extra arguments to function ReadINI");
            try
            {
                variables[line[1]] = OblivionINI.GetINIValue(line[2], line[3]);
            }
            catch (Exception e) { variables[line[1]] = e.Message; }
        }

        private static void FunctionReadRenderer(string[] line)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to function 'ReadRendererInfo'");
                return;
            }
            if (line.Length > 3) Warn("Unexpected extra arguments to function 'ReadRendererInfo'");
            try
            {
                variables[line[1]] = "";
            }
            catch (Exception e) { variables[line[1]] = e.Message; }
        }

        private static int iSet(List<string> func)
        {
            if (func.Count == 0) throw new Exception("Empty iSet");
            if (func.Count == 1) return int.Parse(func[0]);
            //check for brackets
            int index;

            index = func.IndexOf("(");
            while (index != -1)
            {
                int count = 1;
                List<string> newfunc = new List<string>();
                for (int i = index + 1; i < func.Count; i++)
                {
                    if (func[i] == "(") count++;
                    else if (func[i] == ")") count--;
                    if (count == 0)
                    {
                        func.RemoveRange(index, (i - index) + 1);
                        func.Insert(index, iSet(newfunc).ToString());
                        break;
                    }
                    else newfunc.Add(func[i]);
                }
                if (count != 0) throw new Exception("Mismatched brackets");
                index = func.IndexOf("(");
            }

            //not
            index = func.IndexOf("not");
            while (index != -1)
            {
                int i = int.Parse(func[index + 1]);
                i = ~i;
                func[index + 1] = i.ToString();
                func.RemoveAt(index);
                index = func.IndexOf("not");
            }

            //and
            index = func.IndexOf("not");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) & int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("not");
            }

            //or
            index = func.IndexOf("or");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) | int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("or");
            }

            //xor
            index = func.IndexOf("xor");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) ^ int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("xor");
            }

            //mod
            index = func.IndexOf("mod");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) % int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("mod");
            }

            //mod
            index = func.IndexOf("%");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) % int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("%");
            }

            //power
            index = func.IndexOf("^");
            while (index != -1)
            {
                int i = (int)Math.Pow(int.Parse(func[index - 1]), int.Parse(func[index + 1]));
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("^");
            }

            //division
            index = func.IndexOf("/");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) / int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("/");
            }

            //multiplication
            index = func.IndexOf("*");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) * int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("*");
            }

            //add
            index = func.IndexOf("+");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) + int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("+");
            }

            //sub
            index = func.IndexOf("-");
            while (index != -1)
            {
                int i = int.Parse(func[index - 1]) - int.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("-");
            }

            if (func.Count != 1) throw new Exception("Leftovers in function");
            return int.Parse(func[0]);
        }

        private static double fSet(List<string> func)
        {
            if (func.Count == 0) throw new Exception("Empty iSet");
            if (func.Count == 1) return int.Parse(func[0]);
            //check for brackets
            int index;

            index = func.IndexOf("(");
            while (index != -1)
            {
                int count = 1;
                List<string> newfunc = new List<string>();
                for (int i = index; i < func.Count; i++)
                {
                    if (func[i] == "(") count++;
                    else if (func[i] == ")") count--;
                    if (count == 0)
                    {
                        func.RemoveRange(index, i - index);
                        func.Insert(index, fSet(newfunc).ToString());
                        break;
                    }
                    else newfunc.Add(func[i]);
                }
                if (count != 0) throw new Exception("Mismatched brackets");
                index = func.IndexOf("(");
            }

            //sin
            index = func.IndexOf("sin");
            while (index != -1)
            {
                func[index + 1] = Math.Sin(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("sin");
            }

            //cos
            index = func.IndexOf("cos");
            while (index != -1)
            {
                func[index + 1] = Math.Cos(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("cos");
            }

            //tan
            index = func.IndexOf("tan");
            while (index != -1)
            {
                func[index + 1] = Math.Tan(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("tan");
            }

            //sinh
            index = func.IndexOf("sinh");
            while (index != -1)
            {
                func[index + 1] = Math.Sinh(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("sinh");
            }

            //cosh
            index = func.IndexOf("cosh");
            while (index != -1)
            {
                func[index + 1] = Math.Cosh(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("cosh");
            }

            //tanh
            index = func.IndexOf("tanh");
            while (index != -1)
            {
                func[index + 1] = Math.Tanh(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("tanh");
            }

            //exp
            index = func.IndexOf("exp");
            while (index != -1)
            {
                func[index + 1] = Math.Exp(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("exp");
            }

            //log
            index = func.IndexOf("log");
            while (index != -1)
            {
                func[index + 1] = Math.Log10(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("log");
            }

            //ln
            index = func.IndexOf("ln");
            while (index != -1)
            {
                func[index + 1] = Math.Log(double.Parse(func[index + 1])).ToString();
                func.RemoveAt(index);
                index = func.IndexOf("ln");
            }

            //mod
            index = func.IndexOf("mod");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) % double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("mod");
            }

            //mod2
            index = func.IndexOf("%");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) % double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("%");
            }

            //power
            index = func.IndexOf("^");
            while (index != -1)
            {
                double i = Math.Pow(double.Parse(func[index - 1]), double.Parse(func[index + 1]));
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("^");
            }

            //division
            index = func.IndexOf("/");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) / double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("/");
            }

            //multiplication
            index = func.IndexOf("*");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) * double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("*");
            }

            //add
            index = func.IndexOf("+");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) + double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("+");
            }

            //sub
            index = func.IndexOf("-");
            while (index != -1)
            {
                double i = double.Parse(func[index - 1]) - double.Parse(func[index + 1]);
                func[index + 1] = i.ToString();
                func.RemoveRange(index - 1, 2);
                index = func.IndexOf("-");
            }

            if (func.Count != 1) throw new Exception("Leftovers in function");
            return double.Parse(func[0]);
        }

        private static void FunctionSet(string[] line, bool integer)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to function " + (integer ? "iSet" : "fSet"));
                return;
            }
            List<string> func = new List<string>();
            for (int i = 2; i < line.Length; i++) func.Add(line[i]);
            string result;
            try
            {
                if (integer)
                {
                    int i = iSet(func);
                    result = i.ToString();
                }
                else
                {
                    float f = (float)fSet(func);
                    result = f.ToString();
                }
                variables[line[1]] = result;
            }
            catch
            {
                Warn("Invalid arguments to function " + (integer ? "iSet" : "fSet"));
            }
        }

        #endregion
    }
}
