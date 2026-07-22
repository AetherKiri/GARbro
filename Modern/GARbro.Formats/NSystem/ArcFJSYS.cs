using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GameRes.Utility;

namespace GameRes.Formats.NSystem
{
    internal sealed class FjsysArchive : ArcFile
    {
        public readonly string Key;

        public FjsysArchive (ArcView arc, ArchiveFormat impl, ICollection<Entry> dir, string key)
            : base (arc, impl, dir)
        {
            Key = key;
        }
    }

    [Export(typeof(ArchiveFormat))]
    public class FjsysOpener : ArchiveFormat
    {
        public override string Tag { get { return "FJSYS"; } }
        public override string Description { get { return "NSystem engine resource archive"; } }
        public override uint Signature { get { return 0x59534A46; } }
        public override bool IsHierarchic { get { return false; } }
        public override bool CanWrite { get { return false; } }

        public FjsysOpener ()
        {
            Extensions = new[] { "" };
        }

        public override ArcFile TryOpen (ArcView file)
        {
            if (file.View.ReadByte (4) != 'S')
                return null;
            uint namesSize = file.View.ReadUInt32 (0xC);
            int count = file.View.ReadInt32 (0x10);
            if (!IsSaneCount (count))
                return null;
            uint indexOffset = 0x54;
            uint indexSize = checked ((uint)count * 0x10);
            if (indexOffset + indexSize > file.MaxOffset || namesSize > file.MaxOffset - indexOffset - indexSize)
                return null;
            var names = file.View.ReadBytes (indexOffset + indexSize, namesSize);

            var dir = new List<Entry> (count);
            bool hasScripts = false;
            for (int i = 0; i < count; ++i)
            {
                var nameOffset = file.View.ReadInt32 (indexOffset);
                if (nameOffset < 0 || nameOffset >= names.Length)
                    return null;
                var name = Binary.GetCString (names, nameOffset);
                if (string.IsNullOrEmpty (name))
                    return null;
                var entry = FormatCatalog.Instance.Create<Entry> (name);
                entry.Size = file.View.ReadUInt32 (indexOffset + 4);
                entry.Offset = file.View.ReadInt64 (indexOffset + 8);
                if (!entry.CheckPlacement (file.MaxOffset))
                    return null;
                hasScripts = hasScripts || name.HasExtension (".msd");
                dir.Add (entry);
                indexOffset += 0x10;
            }
            if (!hasScripts)
                return new ArcFile (file, this, dir);

            var title = FormatCatalog.Instance.LookupGame (file.Name);
            if (!string.IsNullOrEmpty (title) && DefaultScheme.MsdPasswords.TryGetValue (title, out var password)
                && !string.IsNullOrEmpty (password))
                return new FjsysArchive (file, this, dir, password);
            return new ArcFile (file, this, dir);
        }

        public override Stream OpenEntry (ArcFile arc, Entry entry)
        {
            var fjsys = arc as FjsysArchive;
            if (fjsys == null || !entry.Name.HasExtension (".msd")
                || arc.File.View.AsciiEqual (entry.Offset, "MSCENARIO FILE  "))
                return arc.File.CreateStream (entry.Offset, entry.Size);
            var input = arc.File.CreateStream (entry.Offset, entry.Size);
            return new InputCryptoStream (input, new MsdTransform (fjsys.Key));
        }

#if NET10_0_OR_GREATER
        static readonly FjsysScheme DefaultScheme = new FjsysScheme {
            MsdPasswords = FjsysKeyDatabase.CreateSchemeKeys ()
        };
#else
        static readonly FjsysScheme DefaultScheme = new FjsysScheme {
            MsdPasswords = new Dictionary<string, string> ()
        };
#endif

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("FJSYS scheme is data-backed and read-only."); }
        }
    }

    [Serializable]
    public class FjsysScheme : ResourceScheme
    {
        public Dictionary<string, string> MsdPasswords;
    }

    internal sealed class MsdTransform : ICryptoTransform
    {
        const int BlockSize = 0x20;
        readonly string m_key;
        readonly MD5 m_md5 = MD5.Create ();
        readonly StringBuilder m_hash = new StringBuilder (BlockSize);
        int m_block;

        public bool CanReuseTransform { get { return false; } }
        public bool CanTransformMultipleBlocks { get { return true; } }
        public int InputBlockSize { get { return BlockSize; } }
        public int OutputBlockSize { get { return BlockSize; } }

        public MsdTransform (string key)
        {
            m_key = key;
        }

        public int TransformBlock (byte[] inputBuffer, int inputOffset, int inputCount,
                                   byte[] outputBuffer, int outputOffset)
        {
            int blockCount = inputCount / BlockSize;
            for (int i = 0; i < blockCount; ++i)
            {
                TransformChunk (inputBuffer, inputOffset, BlockSize, outputBuffer, outputOffset);
                inputOffset += BlockSize;
                outputOffset += BlockSize;
            }
            return inputCount;
        }

        public byte[] TransformFinalBlock (byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var output = new byte[inputCount];
            TransformChunk (inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        void TransformChunk (byte[] input, int inputOffset, int count, byte[] output, int outputOffset)
        {
            var chunkKey = Encodings.cp932.GetBytes (m_key + m_block++);
            var hash = m_md5.ComputeHash (chunkKey);
            m_hash.Clear ();
            for (int i = 0; i < hash.Length; ++i)
                m_hash.AppendFormat ("{0:x2}", hash[i]);
            count = Math.Min (m_hash.Length, count);
            for (int i = 0; i < count; ++i)
                output[outputOffset++] = (byte)(input[inputOffset++] ^ m_hash[i]);
        }

        public void Dispose ()
        {
            m_md5.Dispose ();
        }
    }
}
