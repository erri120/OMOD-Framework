using System;
using System.IO;
using SevenZip.Compression.LZMA;

namespace OMODFramework.Classes
{
    internal class SparseFileWriterStream : Stream
    {
        private long position = 0;
        private long length;

        private BinaryReader FileList;

        private string BaseDirectory;
        private string CurrentFile;
        private long FileLength;
        private long Written;
        private FileStream CurrentOutputStream = null;

        internal string GetBaseDirectory() { return BaseDirectory; }

        internal SparseFileWriterStream(Stream fileList)
        {
            using(FileList = new BinaryReader(fileList))
            {
                BaseDirectory = Framework.CreateTempDirectory();
                CreateDirectoryStructure();
                NextFile();
            }
        }

        private void CreateDirectoryStructure()
        {
            long TotalLength = 0;
            while (FileList.PeekChar() != -1)
            {
                string path = FileList.ReadString();
                FileList.ReadInt32();
                TotalLength += FileList.ReadInt64();
                int upto = 0;
                while (true)
                {
                    int i = path.IndexOf('\\', upto);
                    if (i == -1) break;
                    string directory = path.Substring(0, i);
                    if (!Directory.Exists(Path.Combine(BaseDirectory, directory))) Directory.CreateDirectory(Path.Combine(BaseDirectory, directory));
                    upto = i + 1;
                }
            }
            length = TotalLength;
            FileList.BaseStream.Position = 0;
        }

        private void NextFile()
        {
            CurrentFile = FileList.ReadString();
            FileList.ReadUInt32();
            FileLength = FileList.ReadInt64();
            if (CurrentOutputStream != null) CurrentOutputStream.Close();
            if (!Framework.IsSafeFileName(CurrentFile))
            {
                CurrentOutputStream = File.Create(Path.Combine(Framework.TempDir, "IllegalFile"));
            }
            else
            {
                CurrentOutputStream = File.Create(Path.Combine(BaseDirectory, CurrentFile));
            }
            Written = 0;
        }

        public override long Length { get { return length; } }
        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override long Position
        {
            get { return position; }
            set { throw new NotImplementedException("The SparseFileStream does not support seeking"); }
        }
        public override void Flush() { if (CurrentOutputStream != null) CurrentOutputStream.Flush(); }
        public override int Read(byte[] buffer, int offset, int count) { throw new NotImplementedException("The SparseFileStream does not support reading"); }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException("The SparseFileStream does not support seeking"); }

        public override void SetLength(long length) { throw new NotImplementedException("The SparseFileStream does not support length"); }

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

        public override void Close()
        {
            Flush();
            while (FileList.BaseStream.Position < FileList.BaseStream.Length)
            {
                CurrentFile = FileList.ReadString();
                FileList.ReadUInt32();
                FileLength = FileList.ReadInt64();
                if (FileLength > 0) throw new Exception("Compressed data file stream didn't contain enough information to fill all files");
                if (CurrentOutputStream != null) CurrentOutputStream.Close();
                if (!Framework.IsSafeFileName(CurrentFile))
                {
                    CurrentOutputStream = File.Create(Path.Combine(Framework.TempDir, "IllegalFile"));
                }
                else
                {
                    CurrentOutputStream = File.Create(Path.Combine(BaseDirectory, CurrentFile));
                }
            }
            if (CurrentOutputStream != null)
            {
                CurrentOutputStream.Close();
                CurrentOutputStream = null;
            }
        }
    }

    internal class SparseFileReaderStream : Stream
    {
        private long position = 0;
        private long length;

        private string[] FilePaths;
        private int FileCount = 0;
        private FileStream CurrentInputStream = null;
        private long CurrentFileEnd = 0;
        private bool Finished = false;

        internal string CurrentFile { get { return FilePaths[FileCount - 1]; } }

        internal SparseFileReaderStream(string[] filePaths)
        {
            length = 0;
            foreach (string s in filePaths)
            {
                length += (new FileInfo(s)).Length;
            }
            FilePaths = filePaths;
            NextFile();
        }

        private bool NextFile()
        {
            if (CurrentInputStream != null) CurrentInputStream.Close();
            if (FileCount >= FilePaths.Length)
            {
                CurrentInputStream = null;
                Finished = true;
                return false;
            }
            CurrentInputStream = File.OpenRead(FilePaths[FileCount++]);
            CurrentFileEnd += CurrentInputStream.Length;
            return true;
        }
        public override long Position
        {
            get { return position; }
            set { throw new NotImplementedException("The SparseFileReaderStream does not support seeking"); }
        }
        public override long Length { get { return length; } }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override void Flush() { if (CurrentInputStream != null) CurrentInputStream.Flush(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Finished) return 0;
            int read = 0;
            while (count > CurrentFileEnd - position)
            {
                CurrentInputStream.Read(buffer, offset, (int)(CurrentFileEnd - position));
                offset += (int)(CurrentFileEnd - position);
                count -= (int)(CurrentFileEnd - position);
                read += (int)(CurrentFileEnd - position);
                position += CurrentFileEnd - position;
                if (!NextFile()) return read;
            }
            if (count > 0)
            {
                CurrentInputStream.Read(buffer, offset, count);
                position += count;
                read += count;
            }
            return read;
        }
        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException("The SparseFileReaderStream does not support seeking"); }
        public override void SetLength(long length) { throw new NotImplementedException("The SparseFileReaderStream does not support setting length"); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException("The SparseFileReaderStream does not support writing"); }
        public override void Close()
        {
            Flush();
            if (CurrentInputStream != null)
            {
                CurrentInputStream.Close();
                CurrentInputStream = null;
            }
        }
    }

    internal abstract class CompressionHandler
    {
        private readonly static SevenZipHandler SevenZip = new SevenZipHandler();
        //private static ICSharpCode.SharpZipLib.Checksum.Crc32 CRC32 = new ICSharpCode.SharpZipLib.Checksum.Crc32();

        internal static string DecompressFiles(Stream FileList, Stream CompressedStream, CompressionType type)
        {
            switch (type)
            {
                case CompressionType.SevenZip: return SevenZip.DecompressAll(FileList, CompressedStream);
                case CompressionType.Zip: throw new NotImplementedException();
                default: throw new Exception("Unrecognised compression type!");
            }
        }

        protected abstract string DecompressAll(Stream FileList, Stream CompressedStream);
    }

    internal class SevenZipHandler : CompressionHandler
    {
        protected override string DecompressAll(Stream FileList, Stream CompressedStream)
        {
            SparseFileWriterStream sfs = new SparseFileWriterStream(FileList);
            byte[] buffer = new byte[5];
            Decoder decoder = new Decoder();
            CompressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);
            SevenZip.ICodeProgress pb = null;
            try
            {
                decoder.Code(CompressedStream, sfs, CompressedStream.Length - CompressedStream.Position, sfs.Length, pb);
            }
            finally
            {
                sfs.Close();
            }
            return sfs.GetBaseDirectory();
        }
    }
}
