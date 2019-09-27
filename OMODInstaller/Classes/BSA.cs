using System;
using System.IO;
using BSAList = System.Collections.Generic.List<OblivionModManager.Classes.BSAArchive>;
using HashTable = System.Collections.Generic.Dictionary<ulong, OblivionModManager.Classes.BSAArchive.BSAFileInfo>;


namespace OblivionModManager.Classes
{
    class BSAArchive
    {
        [Flags]
        private enum FileFlags : int { Meshes = 1, Textures = 2}

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
                    byte[] b = new byte[size - 4];
                    byte[] output = new byte[bsa.br.ReadUInt32()];
                    bsa.br.Read(b, 0, size - 4);

                    ICSharpCode.SharpZipLib.Zip.Compression.Inflater inf = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater();
                    inf.SetInput(b, 0, b.Length);
                    inf.Inflate(output);

                    return output;
                }
                else
                {
                    return bsa.br.ReadBytes(size);
                }
            }
        }

        private struct BSAFileInfo4
        {
            internal string path;
            internal readonly ulong hash;
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
            internal readonly ulong hash;
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
            internal readonly int directorySize;
            internal readonly int archiveFlags;
            internal readonly int folderCount;
            internal readonly int fileCount;
            internal readonly int totalFolderNameLength;
            internal readonly int totalFileNameLength;
            internal readonly FileFlags fileFlags;

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

            internal bool ContainsMeshes { get { return (fileFlags & FileFlags.Meshes) != 0; } }
            internal bool ContainsTextures { get { return (fileFlags & FileFlags.Textures) != 0; } }
        }

        private BinaryReader br;
        private string name;
        private bool defaultCompressed;
        private static bool Loaded = false;

        private BSAArchive(string path, bool populateAll)
        {
            name = Path.GetFileNameWithoutExtension(path).ToLower();
            BSAHeader4 header;
            br = new BinaryReader(File.OpenRead(path), System.Text.Encoding.Default);
            header = new BSAHeader4(br);
            if (header.bsaVersion != 0x67 || (!populateAll && !header.ContainsMeshes && !header.ContainsTextures))
            {
                br.Close();
                return;
            }
            defaultCompressed = (header.archiveFlags & 0x100) > 0;

            //Read folder info
            BSAFolderInfo4[] folderInfo = new BSAFolderInfo4[header.folderCount];
            BSAFileInfo4[] fileInfo = new BSAFileInfo4[header.fileCount];
            for (int i = 0; i < header.folderCount; i++) folderInfo[i] = new BSAFolderInfo4(br);
            int count = 0;
            for (uint i = 0; i < header.folderCount; i++)
            {
                folderInfo[i].path = new string(br.ReadChars(br.ReadByte() - 1));
                br.BaseStream.Position++;
                folderInfo[i].offset = count;
                for (int j = 0; j < folderInfo[i].count; j++) fileInfo[count + j] = new BSAFileInfo4(br, defaultCompressed);
                count += folderInfo[i].count;
            }
            for (uint i = 0; i < header.fileCount; i++)
            {
                fileInfo[i].path = "";
                char c;
                while ((c = br.ReadChar()) != '\0') fileInfo[i].path += c;
            }

            for (int i = 0; i < header.folderCount; i++)
            {
                for (int j = 0; j < folderInfo[i].count; j++)
                {
                    BSAFileInfo4 fi4 = fileInfo[folderInfo[i].offset + j];
                    string ext = Path.GetExtension(fi4.path);
                    BSAFileInfo fi = new BSAFileInfo(this, (int)fi4.offset, fi4.size);
                    string fpath = Path.Combine(folderInfo[i].path, Path.GetFileNameWithoutExtension(fi4.path));
                    ulong hash = GenHash(fpath, ext);
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

        internal static bool CheckForTexture(string path)
        {
            if (!Loaded) Load(false);

            if (File.Exists("data\\" + path)) return true;
            path = path.ToLower().Replace('/', '\\');
            string ext = Path.GetExtension(path);
            ulong hash = GenHash(Path.ChangeExtension(path, null), ext);
            if (Textures.ContainsKey(hash)) return true;
            return false;
        }

        internal static byte[] GetMesh(string path)
        {
            if (!Loaded) Load(false);

            path = path.ToLower().Replace('/', '\\');
            string ext = Path.GetExtension(path);
            if (ext == ".nif")
            {
                if (File.Exists("data\\" + path)) return File.ReadAllBytes("data\\" + path);
                ulong hash = GenHash(Path.ChangeExtension(path, null), ext);
                if (!Meshes.ContainsKey(hash)) return null;
                return Meshes[hash].GetRawData();
            }
            return null;
        }

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
                    ((file.Length > 2 ? (byte)file[file.Length - 2] : (byte)0) * 0x100) +
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
                    byte a = (byte)(((i & 0xfc) << 5) + (byte)((hash & 0xff000000) >> 24));
                    byte b = (byte)(((i & 0xfe) << 6) + (byte)(hash & 0xff));
                    byte c = (byte)((i << 7) + (byte)((hash & 0xff00) >> 8));
                    hash -= hash & 0xFF00FFFF;
                    hash += (uint)((a << 24) + b + (c << 8));
                }
            }
            return hash;
        }

        private static uint GenHash2(string s)
        {
            uint hash = 0;
            for (int i = 0; i < s.Length; i++)
            {
                hash *= 0x1003f;
                hash += (byte)s[i];
            }
            return hash;
        }

        private void Dispose()
        {
            if (br != null)
            {
                br.Close();
                br = null;
            }
        }

        private static void Load(bool populateAll)
        {
            foreach (string s in Directory.GetFiles("data", "*.bsa")) new BSAArchive(s, populateAll);
            Loaded = true;
        }

        internal static void Clear()
        {
            foreach (BSAArchive BSA in LoadedArchives) BSA.Dispose();
            Meshes.Clear();
            Textures.Clear();
            All.Clear();
            Loaded = false;
        }

        internal static byte[] GetFileFromBSA(string bsa, string path)
        {
            if (!Loaded) Load(true);

            ulong hash = GenHash(path);
            if (!All.ContainsKey(hash)) return null;
            bsa = bsa.ToLower();
            BSAFileInfo fi = All[hash];
            if (fi.bsa.name != bsa) return null;
            return fi.GetRawData();
        }

        internal static byte[] GetFileFromBSA(string path)
        {
            if (!Loaded) Load(true);

            ulong hash = GenHash(path);
            if (!All.ContainsKey(hash)) return null;
            return All[hash].GetRawData();
        }
    }
}
