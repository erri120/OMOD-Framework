﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using CommandLine;

namespace OMODExtractor
{
    public class OMODExtract
    {
        public static Utils utils = new Utils();

        // currently a cli application
        static void Main(String[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                string source = o.InputFile;
                string dest = o.OutputDir;
                utils.DeleteDir(dest);
                Directory.CreateDirectory(dest);
                if (o.UseSevenZip)
                {
                    if (File.Exists("7z.exe"))
                    {
                        if (source.Contains(".omod"))
                        {
                            File.Copy(source, dest+ "\\" + source);
                        }
                        else
                        {
                            Console.Write($"Extracting {source} using 7zip\n");
                            var info = new ProcessStartInfo
                            {
                                FileName = "7z.exe",
                                Arguments = $"x -bsp1 -y -o\"{dest}\" \"{source}\"",
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            var p = new Process
                            {
                                StartInfo = info
                            };
                            p.Start();
                            try
                            {
                                p.PriorityClass = ProcessPriorityClass.BelowNormal;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                            p.WaitForExit();
                            Console.Write($"Archive extracted\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("7z.exe not found!");
                        utils.Exit(1);
                    }
                }
                else
                {
                    Console.WriteLine("The usage of 7zip was set to false, expecting extracted files in "+dest);
                    if (!Directory.Exists(dest))
                    {
                        Console.WriteLine($"The output directory {dest} does not exist!");
                        utils.Exit(2);
                    }
                    if (source.Contains(".omod"))
                    {
                        File.Copy(source, dest + "\\" + source);
                    }
                }

                string outputDir = Path.Combine(Directory.GetCurrentDirectory(), dest);
                outputDir += "\\";
                string[] allOMODFiles = Directory.GetFiles(outputDir, "*.omod", SearchOption.TopDirectoryOnly);

                OMOD omod = null;

                if (allOMODFiles.Length == 1)
                {
                    omod = new OMOD(allOMODFiles[0], outputDir);
                }else if (allOMODFiles.Length == 0)
                {
                    Console.WriteLine("No .omod files found in " + outputDir);
                    utils.Exit(3);
                }else if (allOMODFiles.Length > 1)
                {
                    Console.WriteLine("Multiple .omod files found in " + outputDir);
                    utils.Exit(4);
                }

                if (o.ExtractConfig)
                {
                    omod.SaveConfig();
                }
                if (o.ExtractReadme)
                {
                    omod.SaveFile("readme");
                }
                if (o.ExtractScript)
                {
                    omod.SaveFile("script");
                }
                if (o.ExtractData)
                {
                    omod.ExtractData();
                }
                if (o.ExtractPlugins)
                {
                    omod.ExtractPlugins();
                }

                utils.Exit(0);
            });
        }

        internal class OMOD
        {
            private string path;
            private string basedir;
            private string tempdir;
            private ZipFile ModFile;

            internal OMOD(string path_, string basedir_)
            {
                path = path_;
                ModFile = new ZipFile(path);
                basedir = basedir_;
                tempdir = basedir + "temp\\";
                Directory.CreateDirectory(tempdir);
                utils.AddTempDir(tempdir);
            }

            internal void ExtractData()
            {
                Console.WriteLine("Extracting data from data.crc...");
                string DataPath = GetDataFiles();
                Directory.Move(DataPath, basedir+"data");
                Console.WriteLine("Data extracted to "+basedir+"data");
            }

            internal void ExtractPlugins()
            {
                Console.WriteLine("Extracting plugins from plugins.crc...");
                string PluginsPath = GetPlugins();
                if (PluginsPath != null)
                {
                    Directory.Move(PluginsPath, basedir + "plugins");
                    Console.WriteLine("Plugins extracted to " + basedir + "plugins");
                }
                else
                {
                    if (PluginsPath == null) Console.WriteLine("No plugins found.");
                }
            }

            /// <summary>
            ///     Reads the plugins.crc file and returns the a list of all plugins or
            ///     an empty string if no plugins are found
            /// </summary>
            /// <returns></returns>
            private string[] GetPluginList()
            {
                Stream TempStream = ExtractWholeFile("plugins.crc");
                if (TempStream == null) return new string[0];
                BinaryReader br = new BinaryReader(TempStream);
                List<string> ar = new List<string>();
                while (br.PeekChar() != -1)
                {
                    ar.Add(br.ReadString());
                    br.ReadInt32();
                    br.ReadInt64();
                }
                br.Close();
                return ar.ToArray();
            }

            /// <summary>
            ///     Reads the data.crc file and returns the a list of all files or
            ///     an empty string if no files are found
            /// </summary>
            /// <returns></returns>
            private string[] GetDataList()
            {
                Stream TempStream = ExtractWholeFile("data.crc");
                if (TempStream == null) return new string[0];
                BinaryReader br = new BinaryReader(TempStream);
                List<string> ar = new List<string>();
                while (br.PeekChar() != -1)
                {
                    string s = br.ReadString();
                    ar.Add(s);
                    br.ReadUInt32();
                    br.ReadInt64();
                }
                br.Close();
                return ar.ToArray();
            }

            /// <summary>
            ///     Returns the path to the directory containing the extracted plugins from plugins.crc
            /// </summary>
            /// <returns></returns>
            internal string GetPlugins()
            {
                return ParseCompressedStream("plugins.crc", "plugin");
            }

            /// <summary>
            ///     Returns the path to the directory containing the extracted files from data.crc
            /// </summary>
            /// <returns></returns>
            internal string GetDataFiles()
            {
                return ParseCompressedStream("data.crc", "data");
            }

            /// <summary>
            ///     Extracts the provided .crc file and returns the path to the output
            /// </summary>
            /// <param name="fileList">The crc file, either data.crc or plugins.crc</param>
            /// <param name="compressedStream">Name of the Stream, just use data for data.crc and plugins for plugins.crc</param>
            /// <returns></returns>
            private string ParseCompressedStream(string fileList, string compressedStream)
            {
                string path;
                Stream FileList = ExtractWholeFile(fileList);
                if (FileList == null) return null;
                Stream CompressedStream = ExtractWholeFile(compressedStream);
                path = CompressionHandler.DecompressFiles(FileList, CompressedStream, CompressionHandler.CompressionType.SevenZip);
                FileList.Close();
                CompressedStream.Close();
                return path;
            }

            /// <summary>
            ///     Writes a file somewhere
            /// </summary>
            /// <param name="entry">Path to the output file</param>
            internal void SaveFile(string entry)
            {
                Console.WriteLine("Saving " + entry + " to " + entry + ".txt...");
                string result = null;
                string s = "";
                Stream st = ExtractWholeFile(entry, ref s);
                BinaryReader br = null;

                try
                {
                    br = new BinaryReader(st);
                    result = br.ReadString();
                }
                finally
                {
                    if (br != null) br.Close();
                    utils.SaveToFile(result, basedir + entry + ".txt");
                    Console.WriteLine(entry + " was saved");
                }
            }

            /// <summary>
            ///     Writes the entire config file to config.txt in the output folder
            /// </summary>
            internal void SaveConfig()
            {
                Console.WriteLine("Saving config to config.txt");
                string result = null;
                string s = "";
                Stream st = ExtractWholeFile("config", ref s);
                BinaryReader br = null;

                try
                {
                    br = new BinaryReader(st);
                    byte version = br.ReadByte();
                    string modName = br.ReadString();
                    int majorVersion = br.ReadInt32();
                    int minorVersion = br.ReadInt32();
                    string author = br.ReadString();
                    string email = br.ReadString();
                    string website = br.ReadString();
                    string description = br.ReadString();
                    DateTime creationTime;
                    CompressionHandler.CompressionType CompType;
                    int buildVersion;
                    if (version >= 2)
                    {
                        creationTime = DateTime.FromBinary(br.ReadInt64());
                    }
                    else
                    {
                        string sCreationTime = br.ReadString();
                        if (!DateTime.TryParseExact(sCreationTime, "dd/MM/yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out creationTime))
                        {
                            creationTime = new DateTime(2006, 1, 1);
                        }
                    }
                    if (description == "") description = "No description";
                    CompType = (CompressionHandler.CompressionType)br.ReadByte();
                    if (version >= 1)
                    {
                        buildVersion = br.ReadInt32();
                    }
                    else buildVersion = -1;

                    result = $"version: {version}\n" +
                        $"Modname: {modName}\n" +
                        $"Majorversion: {majorVersion}\n" +
                        $"Minorversion: {minorVersion}\n" +
                        $"Author: {author}\n" +
                        $"Email: {email}\n" +
                        $"Website: {website}\n" +
                        $"Description: {description}\n" +
                        $"Creationtime: {creationTime}\n" +
                        $"buildversion: {buildVersion}";
                }
                finally
                {
                    if (br != null) br.Close();
                    utils.SaveToFile(result, basedir + "config.txt");
                    Console.WriteLine("Config was saved.");
                }
            }

            private Stream ExtractWholeFile(string s)
            {
                string s2 = null;
                return ExtractWholeFile(s, ref s2);
            }

            private Stream ExtractWholeFile(string s, ref string path)
            {
                ZipEntry ze = ModFile.GetEntry(s);
                if (ze == null) return null;
                return ExtractWholeFile(ze, ref path);
            }

            /// <summary>
            ///     Extracts a file from the omod archive and returns a readable Stream,
            ///     also creates a temp folder to store the data in
            /// </summary>
            /// <param name="ze">The name of the file inside the omod archive (config,readme,...)</param>
            /// <param name="path"></param>
            /// <returns></returns>
            private Stream ExtractWholeFile(ZipEntry ze, ref string path)
            {
                Stream file = ModFile.GetInputStream(ze);
                Stream TempStream;
                if (path != null || ze.Size > 67108864)
                {
                    TempStream = CreateTempFile(out path);
                }
                else
                {
                    TempStream = new MemoryStream((int)ze.Size);
                }
                byte[] buffer = new byte[4096];
                int i;
                while ((i = file.Read(buffer, 0, 4096)) > 0)
                {
                    TempStream.Write(buffer, 0, i);
                }
                TempStream.Position = 0;
                return TempStream;
            }

            internal FileStream CreateTempFile()
            {
                string s;
                return CreateTempFile(out s);
            }
            internal FileStream CreateTempFile(out string path)
            {
                int i = 0;
                for (i = 0; i < 32000; i++)
                {
                    if (!File.Exists(tempdir + "tmp" + i.ToString()))
                    {
                        path = tempdir + "tmp" + i.ToString();
                        return File.Create(path);
                    }
                }
                throw new Exception("Could not create temp file because directory is full");
            }
        }

        public static string CreateTempDirectory()
        {
            string tempdir = Directory.GetCurrentDirectory()+"\\temp\\";
            utils.AddTempDir(tempdir);
            for (int i = 0; i < 32000; i++)
            {
                if (!Directory.Exists(tempdir + i.ToString()))
                {
                    Directory.CreateDirectory(tempdir + i.ToString() + "\\");
                    return tempdir + i.ToString() + "\\";
                }
            }
            throw new Exception("Could not create temp folder because directory is full");
        }
    }
}
