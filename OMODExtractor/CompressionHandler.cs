using System;
using System.IO;
using SevenZip.Compression.LZMA;

namespace OMODExtractor
{
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

        internal SparseFileWriterStream(Stream fileList)
        {
            FileList = new BinaryReader(fileList);
            BaseDirectory = OMODExtract.CreateTempDirectory();
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
                    if (!Directory.Exists(BaseDirectory + directory)) Directory.CreateDirectory(BaseDirectory + directory);
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
            CurrentOutputStream = File.Create(BaseDirectory + CurrentFile);
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
                CurrentOutputStream = File.Create(BaseDirectory + CurrentFile);
            }
            if (CurrentOutputStream != null)
            {
                CurrentOutputStream.Close();
                CurrentOutputStream = null;
            }
        }
    }

    internal abstract class CompressionHandler
    {
        public enum CompressionType : byte { SevenZip, Zip }

        private static SevenZipHandler SevenZip = new SevenZipHandler();
        private static ZipHandler Zip = new ZipHandler();

        internal static string DecompressFiles(Stream FileList, Stream CompressedStream, CompressionType type)
        {
            switch (type)
            {
                case CompressionType.SevenZip: return SevenZip.DecompressAll(FileList, CompressedStream);
                case CompressionType.Zip: return Zip.DecompressAll(FileList, CompressedStream); ;
                default: throw new Exception("Unknown compression type");
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
    internal class ZipHandler : CompressionHandler
    {
        protected override string DecompressAll(Stream FileList, Stream CompressedStream)
        {
            throw new NotImplementedException();
        }
    }
}
