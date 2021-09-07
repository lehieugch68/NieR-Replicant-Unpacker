using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ZstdNet;

namespace NieR_Replicant_Unpacker
{
    public class Unpacker
    {
        #region Structure
        private struct BXON_Header
        {
            public int Magic;
            public int Version;
            public int NameOffset;
            public int DataOffset;
            public int ArchiveCount;
            public int ArchiveTableOffset;
            public int FileCount;
            public int FileTableOffset;
            public string Name;
        }
        private struct BXON_Archive
        {
            public int NameOffset;
            public bool isStreamed;
            public string ArchiveName;
            public List<BXON_File> Files;
        }
        private struct BXON_File
        {
            public int NameOffset;
            public long FileOffset;
            public long CompressedSize;
            public long UncompressedSize;
            public long BufferSize;
            public int ArchiveIndex;
            public bool isCompressed;
            public string FilePath;
        }
        #endregion
        private string _RootDirectory;
        public string RootDirectory
        {
            get { return _RootDirectory; }
        }
        public Unpacker(string directory)
        {
            _RootDirectory = directory;
        }
        private string ReadString(ref BinaryReader reader)
        {
            StringBuilder str = new StringBuilder();
            byte[] ch = reader.ReadBytes(1);
            while (ch[0] != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                str.Append(Encoding.ASCII.GetString(ch));
                ch = reader.ReadBytes(1);
            }
            return str.ToString();
        }
        private BXON_Archive[] GetArchives(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            BXON_Header header = new BXON_Header();
            header.Magic = reader.ReadInt32();
            if (header.Magic != 0x4E4F5842) throw new Exception("Unsupported file format.");
            header.Version = reader.ReadInt32();
            reader.BaseStream.Position += 4;
            header.NameOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
            header.DataOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
            reader.BaseStream.Position = header.NameOffset;
            header.Name = this.ReadString(ref reader);
            reader.BaseStream.Position = header.DataOffset;
            header.ArchiveCount = reader.ReadInt32();
            header.ArchiveTableOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
            header.FileCount = reader.ReadInt32();
            header.FileTableOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
            reader.BaseStream.Position = header.ArchiveTableOffset;
            BXON_Archive[] archives = new BXON_Archive[header.ArchiveCount];
            for (int i = 0; i < header.ArchiveCount; i++)
            {
                archives[i].NameOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
                reader.BaseStream.Position += 4;
                archives[i].isStreamed = reader.ReadBoolean();
                reader.BaseStream.Position += 3;
                long temp = reader.BaseStream.Position;
                reader.BaseStream.Position = archives[i].NameOffset;
                archives[i].ArchiveName = this.ReadString(ref reader);
                archives[i].Files = new List<BXON_File>();
                reader.BaseStream.Position = temp;
            }
            reader.BaseStream.Position = header.FileTableOffset;
            for (int i = 0; i < header.FileCount; i++)
            {
                BXON_File file = new BXON_File();
                reader.BaseStream.Position += 4;
                file.NameOffset = (int)reader.BaseStream.Position + reader.ReadInt32();
                file.FileOffset = reader.ReadUInt32();
                file.CompressedSize = reader.ReadInt32();
                file.UncompressedSize = reader.ReadInt32();
                file.BufferSize = reader.ReadInt32();
                file.ArchiveIndex = Convert.ToInt32(reader.ReadByte());
                file.isCompressed = reader.ReadBoolean();
                reader.BaseStream.Position += 2;
                long temp = reader.BaseStream.Position;
                reader.BaseStream.Position = file.NameOffset;
                file.FilePath = this.ReadString(ref reader);
                archives[file.ArchiveIndex].Files.Add(file);
                reader.BaseStream.Position = temp;
            }
            return archives;
        }
        private byte[] Decompress(byte[] input)
        {
            Decompressor decompressor = new Decompressor();
            byte[] decompressedData = decompressor.Unwrap(input);
            return decompressedData;
        }
        public void Unpack(string des, string infoDir)
        {
            string info = Path.Combine(_RootDirectory, infoDir, "info.arc");
            if (!File.Exists(info)) throw new Exception("ToC file not found.");
            byte[] bytes = File.ReadAllBytes(info);
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream);
            uint magic = reader.ReadUInt32();
            if (magic == 0x4E4F5842) { }
            else if (magic == 0xFD2FB528)
            {
                reader.Close();
                stream.Close();
                byte[] decompressedData = Decompress(bytes);
                stream = new MemoryStream(decompressedData);
                reader = new BinaryReader(stream);
            }
            else throw new Exception("Unsupported file format.");
            BXON_Archive[] archives = GetArchives(ref reader);
            reader.Close();
            stream.Close();
            foreach (var archive in archives)
            {
                string arcPath = Path.Combine(_RootDirectory, infoDir, archive.ArchiveName);
                BinaryReader arcReader = archive.isStreamed ? new BinaryReader(File.OpenRead(arcPath)) : new BinaryReader(new MemoryStream(Decompress(File.ReadAllBytes(arcPath))));
                foreach (var file in archive.Files)
                {
                    Console.WriteLine($"{archive.ArchiveName} - {file.FilePath}");
                    string dir = Path.Combine(des, Path.GetDirectoryName(file.FilePath));
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    arcReader.BaseStream.Position = file.FileOffset * 0x10;
                    string filePath = Path.Combine(des, file.FilePath);
                    if (file.isCompressed)
                    {
                        byte[] compressedData = arcReader.ReadBytes((int)file.CompressedSize);
                        byte[] decompressedData = Decompress(compressedData);
                        File.WriteAllBytes(filePath, decompressedData);
                    }
                    else
                    {
                        File.WriteAllBytes(filePath, arcReader.ReadBytes((int)(file.UncompressedSize + file.BufferSize)));
                    }
                }
                arcReader.Close();
            }
        }
    }
}
