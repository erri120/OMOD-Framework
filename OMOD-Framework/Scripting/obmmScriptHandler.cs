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


        internal static ScriptReturnData Execute(Framework f, string InputScript, string DataPath, string PluginsPath,
            bool showWarnings, Action<string> warn, Func<string, string, int> dialogYesNo,
            Func<string, bool> existsFile, Func<string, System.Diagnostics.FileVersionInfo> getFileVersion, 
            Func<string[], string, bool, string[], string[], int[]> dialogSelect)
        {
            ShowWarnings = showWarnings;
            Warn = warn;
            DialogYesNo = dialogYesNo;
            ExistsFile = existsFile;
            GetFileVersion = getFileVersion;
            DialogSelect = dialogSelect;

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

        #endregion
    }
}
