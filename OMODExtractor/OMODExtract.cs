using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using OMODExtractorDLL;

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
                if (o.IsQuiet)
                {
                    Console.SetOut(TextWriter.Null);
                }
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
                        System.Environment.Exit(1);
                    }
                }
                else
                {
                    if (!Directory.Exists(dest))
                    {
                        Console.WriteLine($"The output directory {dest} does not exist!");
                        System.Environment.Exit(2);
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
                    omod = new OMOD(allOMODFiles[0], outputDir,o.TempDir);
                }else if (allOMODFiles.Length == 0)
                {
                    Console.WriteLine("No .omod files found in " + outputDir);
                    System.Environment.Exit(3);
                }else if (allOMODFiles.Length > 1)
                {
                    Console.WriteLine("Multiple .omod files found in " + outputDir);
                    System.Environment.Exit(4);
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

                System.Environment.Exit(0);
            });
        }
    }
}
