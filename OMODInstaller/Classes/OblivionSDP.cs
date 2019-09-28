using System;
using System.Collections.Generic;
using System.IO;
using Formatter = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;

namespace OblivionModManager.Classes
{
    internal static class OblivionSDP
    {
        private static readonly string Path = Program.DataDir+@"shaders\shaderpackage";
        private const string Ext = ".sdp";
        private static readonly string EditedShaderFile = Program.OutputDir + @"EditedShaders";

        [Serializable]
        private class EditedShader
        {
            internal readonly byte Package;
            internal readonly string Name;
            internal readonly byte[] OldData;
            internal readonly uint CRC; //maybe useles
            internal readonly string Mod;

            internal EditedShader(byte package, string name, byte[] old, uint crc, string mod)
            {
                Package = package;
                Name = name;
                OldData = old;
                CRC = crc;
                Mod = mod;
            }
        }

        private static List<EditedShader> EditedShaders = null;

        private static void Deserialize()
        {
            if (!File.Exists(EditedShaderFile))
            {
                EditedShaders = new List<EditedShader>();
                return;
            }
            Formatter f = new Formatter();
            Stream s = File.OpenRead(EditedShaderFile);
            EditedShaders = (List<EditedShader>)f.Deserialize(s);
            s.Close();
        }

        private static void Serialize()
        {
            Formatter f = new Formatter();
            Stream s = File.Create(EditedShaderFile);
            f.Serialize(s, EditedShaders);
            s.Close();
        }

        private static bool IsEdited(byte package, string shader, out string name)
        {
            foreach (EditedShader es in EditedShaders)
            {
                if (es.Package == package && es.Name == shader) { name = es.Mod; return true; }
            }
            name = null;
            return false;
        }

        private static bool ReplaceShader(string file, string shader, byte[] newdata, uint CRC, out byte[] OldData)
        {
            DateTime timeStamp = File.GetLastWriteTime(file);
            File.Delete(Program.TempDir + "tempshader"); //does this actually exists?
            File.Move(file, Program.TempDir + "tempshader");
            bool found = false;

            using (BinaryReader br = new BinaryReader(File.OpenRead(Program.TempDir+"tempshader"), System.Text.Encoding.Default))
            {
                using(BinaryWriter bw = new BinaryWriter(File.Create(file), System.Text.Encoding.Default))
                {
                    bw.Write(br.ReadInt32());
                    int num = br.ReadInt32();
                    bw.Write(num);
                    long sizeoffset = br.BaseStream.Position;
                    bw.Write(br.ReadInt32());
                    
                    OldData = null;
                    
                    for(int i = 0; i < num; i++)
                    {
                        char[] name = br.ReadChars(0x100);
                        int size = br.ReadInt32();
                        byte[] data = br.ReadBytes(size);
                        bw.Write(name);
                        string sName = "";
                        for (int i2 = 0; i2 < 100; i2++) { if (name[i2] == '\0') break; sName += name[i2]; }
                        sName = sName.ToLower();
                        if (!found && sName == shader && (CRC == 0 || CompressionHandler.CRC(data) == CRC))
                        {
                            bw.Write(newdata.Length);
                            bw.Write(newdata);
                            found = true;
                            OldData = data;
                        }
                        else
                        {
                            bw.Write(size);
                            bw.Write(data);
                        }
                    }

                    bw.BaseStream.Position = sizeoffset;
                    bw.Write(bw.BaseStream.Length - 12);
                }
            }
            File.Delete(Program.TempDir + "tempshader");
            File.SetLastWriteTime(file, timeStamp);
            return found;
        }

        internal static bool EditShader(byte package, string name, string newshader, string mod)
        {
            Deserialize();
            bool result = false;
            name = name.ToLower();
            string path = Path + package.ToString().PadLeft(3, '0') + Ext;

            if(File.Exists(newshader) && File.Exists(path))
            {
                string oldMod;
                byte[] NewData = File.ReadAllBytes(newshader);
                byte[] OldData;
                if(ReplaceShader(path, name, NewData, 0, out OldData))
                {
                    EditedShaders.Add(new EditedShader(package, name, OldData, CompressionHandler.CRC(NewData), mod));
                    result = true;
                }
            }
            Serialize();
            return result;
        }
    }
}
