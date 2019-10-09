using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using BSAList = System.Collections.Generic.List<OMODFramework.Classes.BSAArchive>;
using HashTable = System.Collections.Generic.Dictionary<ulong, OMODFramework.Classes.BSAArchive.BSAFileInfo>;


namespace OMODFramework.Classes
{
    class BSAArchive
    {
        [Flags]
        private enum FileFlags
        { Meshes = 1, Textures = 2 }

        internal struct BSAFileInfo
        {
            internal readonly BSAArchive bsa;
            internal readonly int offset;
            internal readonly int size;
            internal readonly bool compressed;

            internal BSAFileInfo(BSAArchive _bsa, int _offset, int _size)
            {
                bsa = _bsa;
                offset = _offset;
                size = _size;

                if ((size & (1 << 30)) != 0)
                {
                    size ^= 1 << 30;
                    compressed = !bsa.defaultCompressed;
                }
                else compressed = bsa.defaultCompressed;
            }

            internal byte[] GetRawData()
            {
                bsa.br.BaseStream.Seek(offset, SeekOrigin.Begin);
                if (compressed)
                {
                    var b = new byte[size - 4];
                    var output = new byte[bsa.br.ReadUInt32()];
                    bsa.br.Read(b, 0, size - 4);

                    var inf = new Inflater();
                    inf.SetInput(b, 0, b.Length);
                    inf.Inflate(output);

                    return output;
                }

                return bsa.br.ReadBytes(size);
            }
        }

        private struct BSAFileInfo4
        {
            internal string path;
            private readonly ulong hash;
            internal readonly int size;
            internal readonly uint offset;

            internal BSAFileInfo4(BinaryReader br, bool defaultCompressed)
            {
                path = null;

                hash = br.ReadUInt64();
                size = br.ReadInt32();
                offset = br.ReadUInt32();

                if (defaultCompressed) size ^= (1 << 30);
            }
        }

        private struct BSAFolderInfo4
        {
            internal string path;
            private readonly ulong hash;
            internal readonly int count;
            internal int offset;

            internal BSAFolderInfo4(BinaryReader br)
            {
                path = null;
                offset = 0;

                hash = br.ReadUInt64();
                count = br.ReadInt32();
                //offset=br.ReadInt32();
            }
        }

        private struct BSAHeader4
        {
            internal readonly uint bsaVersion;
            private readonly int directorySize;
            internal readonly int archiveFlags;
            internal readonly int folderCount;
            internal readonly int fileCount;
            private readonly int totalFolderNameLength;
            private readonly int totalFileNameLength;
            private readonly FileFlags fileFlags;

            internal BSAHeader4(BinaryReader br)
            {
                br.BaseStream.Position += 4;
                bsaVersion = br.ReadUInt32();
                directorySize = br.ReadInt32();
                archiveFlags = br.ReadInt32();
                folderCount = br.ReadInt32();
                fileCount = br.ReadInt32();
                totalFolderNameLength = br.ReadInt32();
                totalFileNameLength = br.ReadInt32();
                fileFlags = (FileFlags)br.ReadInt32();
            }

            internal bool ContainsMeshes => (fileFlags & FileFlags.Meshes) != 0;
            internal bool ContainsTextures => (fileFlags & FileFlags.Textures) != 0;
        }

        private readonly BinaryReader br;
        private readonly string name;
        private readonly bool defaultCompressed;
        private static bool Loaded;

        private BSAArchive(string path, bool populateAll)
        {
            name = Path.GetFileNameWithoutExtension(path).ToLower();
            br = new BinaryReader(File.OpenRead(path), Encoding.Default);
            var header = new BSAHeader4(br);
            if (header.bsaVersion != 0x67 || (!populateAll && !header.ContainsMeshes && !header.ContainsTextures))
            {
                br.Close();
                return;
            }
            defaultCompressed = (header.archiveFlags & 0x100) > 0;

            //Read folder info
            var folderInfo = new BSAFolderInfo4[header.folderCount];
            var fileInfo = new BSAFileInfo4[header.fileCount];
            for (var i = 0; i < header.folderCount; i++) folderInfo[i] = new BSAFolderInfo4(br);
            var count = 0;
            for (uint i = 0; i < header.folderCount; i++)
            {
                folderInfo[i].path = new string(br.ReadChars(br.ReadByte() - 1));
                br.BaseStream.Position++;
                folderInfo[i].offset = count;
                for (var j = 0; j < folderInfo[i].count; j++) fileInfo[count + j] = new BSAFileInfo4(br, defaultCompressed);
                count += folderInfo[i].count;
            }
            for (uint i = 0; i < header.fileCount; i++)
            {
                fileInfo[i].path = "";
                char c;
                while ((c = br.ReadChar()) != '\0') fileInfo[i].path += c;
            }

            for (var i = 0; i < header.folderCount; i++)
            {
                for (var j = 0; j < folderInfo[i].count; j++)
                {
                    var fi4 = fileInfo[folderInfo[i].offset + j];
                    var ext = Path.GetExtension(fi4.path);
                    var fi = new BSAFileInfo(this, (int)fi4.offset, fi4.size);
                    var fpath = Path.Combine(folderInfo[i].path, Path.GetFileNameWithoutExtension(fi4.path));
                    var hash = GenHash(fpath, ext);
                    if (ext == ".nif")
                    {
                        Meshes[hash] = fi;
                    }
                    else if (ext == ".dds")
                    {
                        Textures[hash] = fi;
                    }
                    All[hash] = fi;
                }
            }
            LoadedArchives.Add(this);
        }

        private static readonly BSAList LoadedArchives = new BSAList();
        private static readonly HashTable Meshes = new HashTable();
        private static readonly HashTable Textures = new HashTable();
        private static readonly HashTable All = new HashTable();

        private static ulong GenHash(string file)
        {
            file = file.ToLower().Replace('/', '\\');
            return GenHash(Path.ChangeExtension(file, null), Path.GetExtension(file));
        }
        private static ulong GenHash(string file, string ext)
        {
            file = file.ToLower();
            ext = ext.ToLower();
            ulong hash = 0;
            if (file.Length > 0)
            {
                hash = (ulong)(
                   (((byte)file[file.Length - 1]) * 0x1) +
                    ((file.Length > 2 ? (byte)file[file.Length - 2] : 0) * 0x100) +
                     (file.Length * 0x10000) +
                    (((byte)file[0]) * 0x1000000)
                );
            }
            if (file.Length > 3)
            {
                hash += (ulong)(GenHash2(file.Substring(1, file.Length - 3)) * 0x100000000);
            }
            if (ext.Length > 0)
            {
                hash += (ulong)(GenHash2(ext) * 0x100000000);
                byte i = 0;
                switch (ext)
                {
                    case ".nif": i = 1; break;
                    //case ".kf": i=2; break;
                    case ".dds": i = 3; break;
                        //case ".wav": i=4; break;
                }
                if (i != 0)
                {
                    var a = (byte)(((i & 0xfc) << 5) + (byte)((hash & 0xff000000) >> 24));
                    var b = (byte)(((i & 0xfe) << 6) + (byte)(hash & 0xff));
                    var c = (byte)((i << 7) + (byte)((hash & 0xff00) >> 8));
                    hash -= hash & 0xFF00FFFF;
                    hash += (uint)((a << 24) + b + (c << 8));
                }
            }
            return hash;
        }

        private static uint GenHash2(string s)
        {
            uint hash = 0;
            foreach (var t in s)
            {
                hash *= 0x1003f;
                hash += (byte)t;
            }
            return hash;
        }

        private static void Load(bool populateAll)
        {
            foreach (var s in Directory.GetFiles("data", "*.bsa")) new BSAArchive(s, populateAll);
            Loaded = true;
        }

        internal static byte[] GetFileFromBSA(string bsa, string path)
        {
            if (!Loaded) Load(true);

            var hash = GenHash(path);
            if (!All.ContainsKey(hash)) return null;
            bsa = bsa.ToLower();
            var fi = All[hash];
            if (fi.bsa.name != bsa) return null;
            return fi.GetRawData();
        }

        internal static byte[] GetFileFromBSA(string path)
        {
            if (!Loaded) Load(true);

            var hash = GenHash(path);
            if (!All.ContainsKey(hash)) return null;
            return All[hash].GetRawData();
        }
    }
}
