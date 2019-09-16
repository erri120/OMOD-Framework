using System;
using System.Diagnostics;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using SevenZip.Compression.LZMA;



namespace OMODExtractor
{
    class OMODExtractor
    {
        public enum CompressionType : byte { SevenZip, Zip }
        public enum CompressionLevel : byte { VeryHigh, High, Medium, Low, VeryLow, None}
        static void Main(string[] args)
        {
            if(args.Length == 2)
            {
                string source = args[0];
                string dest = args[1];
                Console.WriteLine($"Extracting {source} using 7zip");

                Extract7Zip(source, dest);

                string outdir = Alphaleonis.Win32.Filesystem.Path.Combine(Alphaleonis.Win32.Filesystem.Directory.GetCurrentDirectory(),dest);
                outdir = outdir + "\\";
                string[] allOmods = System.IO.Directory.GetFiles(outdir, "*.omod", SearchOption.TopDirectoryOnly);
                if(allOmods.Length == 1)
                {
                    string omod = allOmods[0];
                    ZipFile ModFile = new ZipFile(omod);

                    SaveToFile(GetFile(ModFile, ModFile.GetEntry("readme")), outdir + "readme.txt");
                    SaveToFile(GetFile(ModFile, ModFile.GetEntry("script")), outdir + "script.txt");
                    SaveToFile(GetConfig(ModFile), outdir + "config.txt");
                    ExtractData(outdir,ModFile);
                }           
            } 
        }

        public static FileStream CreateTempFile(string temp)
        {
            string s;
            return CreateTempFile(temp,out s);
        }

        public static FileStream CreateTempFile(string temp,out string path)
        {
            int i = 0;
            for (i = 0; i < 32000; i++)
            {
                if (!File.Exists(temp + "tmp" + i.ToString()))
                {
                    path = temp + "tmp" + i.ToString();
                    return File.Create(path);
                }
            }
            throw new Exception("");
        }

        public static void ExtractData(string basedir, ZipFile ModFile)
        {
            string DataPath = ParseCompressedStream(basedir,ModFile, "data,crc", "data");
            Console.WriteLine(DataPath);
        }

        public static string ParseCompressedStream(string basedir, ZipFile ModFile, string fileList, string compressedStream)
        {
            string path = "";
            Stream FileList = ExtractFile(ModFile,ModFile.GetEntry(fileList));
            if (FileList == null) return null;
            Stream CompressedStream = ExtractFile(ModFile, ModFile.GetEntry(compressedStream));
            path = DecompressFiles(basedir,FileList, CompressedStream);
            FileList.Close();
            CompressedStream.Close();
            return path;
        }

        internal class SparseFileWriterStream : Stream
        {
            private long position = 0;
            private long length;

            private BinaryReader FileList;

            private string BaseDirectory;
            private string CurrentFile;
            private uint FileCRC;
            private long FileLength;
            private long Written;
            private FileStream CurrentOutputStream = null;

            internal string GetBaseDirectory()
            {
                return BaseDirectory;
            }

            internal SparseFileWriterStream(Stream fileList,string basedir)
            {
                FileList = new BinaryReader(fileList);
                BaseDirectory = basedir;
                CreateDirectoryStructure();
                NextFile();
            }

            private void CreateDirectoryStructure()
            {
                long TotalLength = 0;
                while (FileList.PeekChar() != -1)
                {
                    string Path = FileList.ReadString();
                    FileList.ReadInt32();
                    TotalLength += FileList.ReadInt64();
                    int upto = 0;
                    while (true)
                    {
                        int i = Path.IndexOf('\\', upto);
                        if (i == -1) break;
                        string directory = Path.Substring(0, i);
                        if (!System.IO.Directory.Exists(BaseDirectory + directory)) System.IO.Directory.CreateDirectory(BaseDirectory + directory);
                        upto = i + 1;
                    }
                }
                length = TotalLength;
                FileList.BaseStream.Position = 0;
            }

            private void NextFile()
            {
                CurrentFile = FileList.ReadString();
                FileCRC = FileList.ReadUInt32();
                FileLength = FileList.ReadInt64();
                if (CurrentOutputStream != null) CurrentOutputStream.Close();
                CurrentOutputStream = System.IO.File.Create(BaseDirectory + CurrentFile);
                Written = 0;
            }

            public override long Position
            {
                get { return position; }
                set { throw new NotImplementedException("The SparseFileStream does not support seeking"); }
            }
            public override long Length { get { return length; } }
            public override bool CanRead { get { return false; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return true; } }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException("The SparseFileStream does not support reading");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                while (Written + count > FileLength)
                {
                    CurrentOutputStream.Write(buffer, offset, (int)(FileLength - Written));
                    offset += (int)(FileLength - Written);
                    count -= (int)(FileLength - Written);
                    NextFile();
                }
                if (count > 0)
                {
                    CurrentOutputStream.Write(buffer, offset, count);
                    Written += count;
                }
            }

            public override void SetLength(long length)
            {
                throw new NotImplementedException("The SparseFileStream does not support length");
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException("The SparseFileStream does not support seeking");
            }

            public override void Flush() { if (CurrentOutputStream != null) CurrentOutputStream.Flush(); }

            public override void Close()
            {
                Flush();
                //Added this to properly create any empty files at the end of the archive
                while (FileList.BaseStream.Position < FileList.BaseStream.Length)
                {
                    CurrentFile = FileList.ReadString();
                    FileCRC = FileList.ReadUInt32();
                    FileLength = FileList.ReadInt64();
                    if (FileLength > 0) throw new Exception("Compressed data file stream didn't contain enough information to fill all files");
                    if (CurrentOutputStream != null) CurrentOutputStream.Close();
                    CurrentOutputStream = System.IO.File.Create(BaseDirectory + CurrentFile);
                }
                if (CurrentOutputStream != null)
                {
                    CurrentOutputStream.Close();
                    CurrentOutputStream = null;
                }
            }
        }

        public static string DecompressAllSevenZip(string basedir, Stream FileList, Stream CompressedStream)
        {
            SparseFileWriterStream sfs = new SparseFileWriterStream(FileList, basedir);
            byte[] buffer = new byte[5];
            Decoder decoder = new Decoder();
            CompressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);
            SevenZip.ICodeProgress pb = null;
            try
            {
                decoder.Code(CompressedStream, sfs, CompressedStream.Length - CompressedStream.Position, sfs.Length,pb);
            }
            finally
            {
                pb = null;
                sfs.Close();
            }
            return sfs.GetBaseDirectory();
        }

        public static string DecompressAllZip(string basedir, Stream FileList, Stream CompressedStream)
        {
            return "";
        }

        public static string DecompressFiles(string basedir, Stream FileList, Stream CompressedStream)
        {
            //switch (type)
            //{
            //    case CompressionType.SevenZip:
                    return DecompressAllSevenZip(basedir, FileList, CompressedStream);
            //    case CompressionType.Zip:
            //        return DecompressAllZip(basedir, FileList, CompressedStream);
           //     default: return "asd";
            //}
        }

        public static bool HasPlugins(ZipFile ModFile)
        {
            return ModFile.GetEntry("plugins.crc") != null;
        }
        
        public static string GetConfig(ZipFile ModFile)
        {
            string result = "";
            ZipEntry config = ModFile.GetEntry("config");
            Stream s = ExtractFile(ModFile,config);
            BinaryReader br = new BinaryReader(s);

            try { 
                byte version = br.ReadByte();
                string modName = br.ReadString();
                int majorVersion = br.ReadInt32();
                int minorVersion = br.ReadInt32();
                string author = br.ReadString();
                string email = br.ReadString();
                string website = br.ReadString();
                string description = br.ReadString();
                DateTime creationTime;
                CompressionType CompType;
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
                CompType = (CompressionType)br.ReadByte();
                if (version >= 1)
                {
                    buildVersion = br.ReadInt32();
                }
                else buildVersion = -1;

                result += $"version: {version}\n" +
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
                br.Close();
            }

            return result;
        }

        public static string GetFile(ZipFile ModFile, ZipEntry entry)
        {
            Stream s = ExtractFile(ModFile, entry);
            if (s == null) return null;
            BinaryReader br = new BinaryReader(s);
            string result = br.ReadString();
            br.Close();
            return result;
        }

        public static bool HasFile(ZipFile ModFile, string file)
        {
            return ModFile.GetEntry(file) != null;
        }
        public static Stream ExtractFile(ZipFile ModFile, ZipEntry ze)
        {
            if (ze == null) return null;
            Stream file = ModFile.GetInputStream(ze);
            Stream TempStream;
            TempStream = new MemoryStream((int)ze.Size);
            byte[] buffer = new byte[4096];
            int i;
            while ((i = file.Read(buffer, 0, 4096)) > 0)
            {
                TempStream.Write(buffer, 0, i);
            }
            TempStream.Position = 0;
            return TempStream;
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

        public static void SaveToFile(string contents, string dest)
        {
            if (System.IO.File.Exists(dest)) System.IO.File.Delete(dest);
            System.IO.File.WriteAllText(dest, contents);
        }

        public static void Extract7Zip(string source, string dest)
        {
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
        }
    }
}
