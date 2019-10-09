using System;
using System.IO;
using SevenZip.Compression.LZMA;

namespace OMODFramework.Classes
{
    internal class SparseFileWriterStream : Stream
    {
        private readonly long position = 0;
        private long length;

        private readonly BinaryReader FileList;

        private readonly string BaseDirectory;
        private string CurrentFile;
        private long FileLength;
        private long Written;
        private FileStream CurrentOutputStream;

        internal string GetBaseDirectory() { return BaseDirectory; }

        internal SparseFileWriterStream(Stream fileList)
        {
            FileList = new BinaryReader(fileList);
            BaseDirectory = Framework.CreateTempDirectory();
            CreateDirectoryStructure();
            NextFile();
        }

        private void CreateDirectoryStructure()
        {
            long TotalLength = 0;
            while (FileList.PeekChar() != -1)
            {
                var path = FileList.ReadString();
                FileList.ReadInt32();
                TotalLength += FileList.ReadInt64();
                var upto = 0;
                while (true)
                {
                    var i = path.IndexOf('\\', upto);
                    if (i == -1) break;
                    var directory = path.Substring(0, i);
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
            CurrentOutputStream?.Close();
            CurrentOutputStream = File.Create(!Framework.IsSafeFileName(CurrentFile) ? Path.Combine(Framework.TempDir, "IllegalFile") : Path.Combine(BaseDirectory, CurrentFile));
            Written = 0;
        }

        public override long Length => length;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Position
        {
            get => position;
            set => throw new NotImplementedException("The SparseFileStream does not support seeking");
        }
        public override void Flush()
        {
            CurrentOutputStream?.Flush();
        }
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
                CurrentOutputStream?.Close();
                CurrentOutputStream = File.Create(!Framework.IsSafeFileName(CurrentFile) ? Path.Combine(Framework.TempDir, "IllegalFile") : Path.Combine(BaseDirectory, CurrentFile));
            }
            if (CurrentOutputStream != null)
            {
                CurrentOutputStream.Close();
                CurrentOutputStream = null;
            }
        }
    }

/*
    internal class SparseFileReaderStream : Stream
    {
        private long position;
        private readonly long length;

        private readonly string[] FilePaths;
        private int FileCount;
        private FileStream CurrentInputStream;
        private long CurrentFileEnd;
        private bool Finished;

        internal string CurrentFile => FilePaths[FileCount - 1];

        internal SparseFileReaderStream(string[] filePaths)
        {
            length = 0;
            foreach (var s in filePaths)
            {
                length += (new FileInfo(s)).Length;
            }
            FilePaths = filePaths;
            NextFile();
        }

        private bool NextFile()
        {
            CurrentInputStream?.Close();
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
            get => position;
            set => throw new NotImplementedException("The SparseFileReaderStream does not support seeking");
        }
        public override long Length => length;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override void Flush()
        {
            CurrentInputStream?.Flush();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Finished) return 0;
            var read = 0;
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
*/

    internal abstract class CompressionHandler
    {
        private static readonly SevenZipHandler SevenZip = new SevenZipHandler();
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
            var sfs = new SparseFileWriterStream(FileList);
            var buffer = new byte[5];
            var decoder = new Decoder();
            CompressedStream.Read(buffer, 0, 5);
            decoder.SetDecoderProperties(buffer);
            try
            {
                decoder.Code(CompressedStream, sfs, CompressedStream.Length - CompressedStream.Position, sfs.Length, null);
            }
            finally
            {
                sfs.Close();
            }
            return sfs.GetBaseDirectory();
        }
    }
}
