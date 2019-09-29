using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OblivionModManager.Scripting
{
    internal static class obmmScriptHandler
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

        private static ScriptReturnData srd;
        private static Dictionary<string, string> variables;

        private static string DataFiles;
        private static string Plugins;
        private static string cLine = "0";
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

        private static void Warn(string Message)
        {
            MessageBox.Show(Message+$" on line {cLine}", "Warning");
        }

        internal static ScriptReturnData Execute(string InputScript, string DataPath, string PluginsPath)
        {
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
            bool Break = false;

            for(int i = 0; i < script.Length || ExtraLines.Count > 0; i++)
            {
                if(ExtraLines.Count > 0)
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

                if(SkipTo != null)
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
            if(line.Length == 1)
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
                            return MessageBox.Show(line[2], "", MessageBoxButtons.YesNo) == DialogResult.Yes;
                        case 4:
                            return MessageBox.Show(line[2], line[3], MessageBoxButtons.YesNo) == DialogResult.Yes;
                        default:
                            Warn("Unexpected arguments after function 'If DialogYesNo'");
                            goto case 4;
                    }
                case "DataFileExists":
                    if(line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If DataFileExists'");
                        return false;
                    }
                    return File.Exists(Program.DataDir + line[2]);
                case "VersionGreaterThan":
                    if(line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If VersionGreaterThan'");
                        return false;
                    }
                    try
                    {
                        Version v = new Version(line[2]+".0");
                        Version v2 = new Version(Program.MajorVersion.ToString() + "." + Program.MinorVersion.ToString() + "." + Program.BuildNumber.ToString() + ".0");
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
                        Version v2 = new Version(Program.MajorVersion.ToString() + "." +
                            Program.MinorVersion.ToString() + "." + Program.BuildNumber.ToString() + ".0");
                        return (v2 < v);
                    }
                    catch
                    {
                        Warn("Invalid argument to function 'If VersionLessThan'");
                        return false;
                    }
                case "ScriptExtenderPresent":
                    if (line.Length > 2) Warn("Unexpected arguments to 'If ScriptExtenderPresent'");
                    return File.Exists(Directory.GetParent(Program.DataDir) + "obse_loader.exe");
                case "ScriptExtenderNewerThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If ScriptExtenderNewerThan'");
                        return false;
                    }
                    if (line.Length > 3) Warn("Unexpected arguments to 'If ScriptExtenderNewerThan'");
                    if (!File.Exists(Directory.GetParent(Program.DataDir)+"obse_loader.exe")) return false;
                    try
                    {
                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Directory.GetParent(Program.DataDir) + "obse_loader.exe");
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
                    return File.Exists(Program.DataDir+@"obse\plugins\obge.dll");
                case "GraphicsExtenderNewerThan":
                    if (line.Length == 2)
                    {
                        Warn("Missing arguments to function 'If GraphicsExtenderNewerThan'");
                        return false;
                    }
                    if (line.Length > 3) Warn("Unexpected arguments to 'If GraphicsExtenderNewerThan'");
                    if (!File.Exists(Program.DataDir + @"obse\plugins\obge.dll")) return false;
                    try
                    {
                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Program.DataDir + @"obse\plugins\obge.dll");
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
                        System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Directory.GetParent(Program.DataDir)+"oblivion.exe");
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

        private static string[] FunctionSelect(string[] line, bool many, bool Previews, bool Descriptions)
        {
            if (line.Length < 3)
            {
                Warn("Missing arguments to function 'Select'");
                return new string[0];
            }

            string[] items;
            string[] previews;
            string[] descs;
            int argsPerOption = 1 + (Previews ? 1 : 0) + (Descriptions ? 1 : 0);

            string title = line[1];
            items = new string[line.Length - 2];
            Array.Copy(line, 2, items, 0, line.Length - 2);
            line = items;

            if (line.Length % argsPerOption != 0)
            {
                Warn("Unexpected extra arguments to 'Select'");
                Array.Resize(ref line, line.Length - line.Length % argsPerOption);
            }

            // Create arrays to pass to the select form
            items = new string[line.Length / argsPerOption];
            previews = Previews ? new string[line.Length / argsPerOption] : null;
            descs = Descriptions ? new string[line.Length / argsPerOption] : null;

            for(int i = 0; i < line.Length / argsPerOption; i++)
            {
                items[i] = line[i * argsPerOption];
                if (Previews)
                {
                    previews[i] = line[i * argsPerOption + 1];
                    if (Descriptions) descs[i] = line[i * argsPerOption + 2];
                }
                else
                {
                    if (Descriptions) descs[i] = line[i * argsPerOption + 1];
                }
            }

            // Check for previews
            if (previews != null)
            {
                for (int i = 0; i < previews.Length; i++)
                {
                    if (previews[i] == "None")
                    {
                        previews[i] = null;
                    }
                    else if (!Program.IsSafeFileName(previews[i]))
                    {
                        Warn($"Preview file path '{previews[i]}' was invalid");
                        previews[i] = null;
                    }
                    else if (!File.Exists(DataFiles + previews[i]))
                    {
                        Warn($"Preview file path '{previews[i]}' does not exist");
                        previews[i] = null;
                    }
                    else
                    {
                        previews[i] = DataFiles + previews[i];
                    }
                }
            }

            // Display the select form
            Forms.SelectForm sf = new Forms.SelectForm(items, title, many, previews, descs);
            try
            {
                sf.ShowDialog();
            } catch (ExecutionCancelledException)
            {
                srd.CancelInstall = true;
                return new string[0];
            }
            string[] result = new string[sf.SelectedIndex.Length];
            for(int i = 0; i < sf.SelectedIndex.Length; i++)
            {
                result[i] = $"Case {items[sf.SelectedIndex[i]]}";
            }
            return result;
        }

        private static string[] FunctionSelectVar(string[] line, bool IsVariable)
        {
            string Func;
            if (IsVariable) Func = " to function 'SelectVar'"; else Func = "to function 'SelectString'";
            if (line.Length < 2)
            {
                Warn("Missing arguments" + Func);
                return new string[0];
            }
            if (line.Length > 2) Warn("Unexpected arguments" + Func);
            if (IsVariable)
            {
                if (!variables.ContainsKey(line[1]))
                {
                    Warn("Invalid argument" + Func + "\nVariable '" + line[1] + "' does not exist");
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
            throw new NotImplementedException();
        }

        private static void FunctionMessage(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionLoadEarly(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionLoadOrder(string[] line, bool LoadAfter)
        {
            throw new NotImplementedException();
        }

        private static void FunctionConflicts(string[] line, bool Conflicts, bool Regex)
        {
            throw new NotImplementedException();
        }

        private static void FunctionModifyInstall(string[] line, bool plugins, bool Install)
        {
            throw new NotImplementedException();
        }

        private static void FunctionModifyInstallFolder(string[] line, bool Install)
        {
            throw new NotImplementedException();
        }

        private static void FunctionRegisterBSA(string[] line, bool Register)
        {
            throw new NotImplementedException();
        }

        private static void FunctionUncheckESP(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSetDeactivationWarning(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionCopyDataFile(string[] line, bool Plugin)
        {
            throw new NotImplementedException();
        }

        private static void FunctionCopyDataFolder(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionPatch(string[] line, bool Plugin)
        {
            throw new NotImplementedException();
        }

        private static void FunctionEditINI(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionEditShader(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSetEspVar(string[] line, bool GMST)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSetEspData(string[] line, Type type)
        {
            throw new NotImplementedException();
        }

        private static void FunctionDisplayFile(string[] line, bool Image)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSetVar(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionGetDirectoryName(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionGetFileName(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionGetFileNameWithoutExtension(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionCombinePaths(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSubstring(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionRemoveString(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionStringLength(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionInputString(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionReadINI(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionReadRenderer(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionEditXMLLine(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionEditXMLReplace(string[] line)
        {
            throw new NotImplementedException();
        }

        private static void FunctionExecLines(string[] line, Queue<string> queue)
        {
            throw new NotImplementedException();
        }

        private static int iSet(List<string> func)
        {
            throw new NotImplementedException();
        }

        private static double fSet(List<string> func)
        {
            throw new NotImplementedException();
        }

        private static void FunctionSet(string[] line, bool integer)
        {
            throw new NotImplementedException();
        }

        #endregion
        }
}
