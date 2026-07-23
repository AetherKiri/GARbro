using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GameRes.Utility;

namespace GameRes.Formats.Cyberworks
{
    [Serializable]
    public sealed class TinkAudioScheme : ResourceScheme
    {
        public Dictionary<uint, byte[]> KnownKeys;
    }

    [Export(typeof(AudioFormat))]
    public sealed class TinkAudio : AudioFormat
    {
        public override string Tag { get { return "OGG/TINK"; } }
        public override string Description { get { return "Cyberworks encrypted OGG audio"; } }
        public override uint Signature { get { return 0x6B6E6954; } }

        static readonly TinkAudioScheme DefaultScheme = new TinkAudioScheme {
            KnownKeys = TinkKeyDatabase.CreateSchemeKeys ()
        };

        public TinkAudio ()
        {
            Signatures = new uint[] { 0x6B6E6954, 0x676E6F53, 0 }; // 'Tink', 'Song'
            Extensions = new string[] { "j0", "k0", "u0" };
        }

        public override ResourceScheme Scheme
        {
            get { return DefaultScheme; }
            set { throw new InvalidOperationException ("TINK scheme is data-backed and read-only."); }
        }

        public override SoundInput TryOpen (IBinaryStream file)
        {
            if (!TinkDecoder.TryDecodeHeader (file, DefaultScheme.KnownKeys, out var header))
                return null;
            Stream input;
            if (header.Length >= file.Length)
                input = new MemoryStream (header);
            else
                input = new PrefixStream (header, new StreamRegion (file.AsStream, file.Position));
            var sound = new OggInput (input);
            if (header.Length >= file.Length)
                file.Dispose();
            return sound;
        }
    }

    internal static class TinkDecoder
    {
        const int MaxHeaderSize = 0xE1F;

        internal static bool TryDecodeHeader (IBinaryStream file,
                                               IReadOnlyDictionary<uint, byte[]> knownKeys,
                                               out byte[] header)
        {
            header = null;
            if (file.Length < 0x10 || file.Length > int.MaxValue)
                return false;
            int headerSize = (int)Math.Min (MaxHeaderSize, file.Length);
            header = new byte[headerSize];
            var initial = file.ReadBytes (0x10);
            if (initial.Length != 0x10)
            {
                header = null;
                return false;
            }
            Buffer.BlockCopy (initial, 0, header, 0, initial.Length);

            byte[] key;
            var signature = LittleEndian.ToUInt32 (header, 0);
            if (!knownKeys.TryGetValue (signature, out key))
            {
                signature = LittleEndian.ToUInt32 (header, 0xC);
                if (!knownKeys.TryGetValue (signature, out key))
                {
                    header = null;
                    return false;
                }
                var shifted = file.ReadBytes (0xC);
                if (shifted.Length != 0xC)
                {
                    header = null;
                    return false;
                }
                Buffer.BlockCopy (shifted, 0, header, 4, shifted.Length);
            }

            var remainder = file.ReadBytes (headerSize - 0x10);
            if (remainder.Length != headerSize - 0x10)
            {
                header = null;
                return false;
            }
            Buffer.BlockCopy (remainder, 0, header, 0x10, remainder.Length);
            header[0] = (byte)'O';
            header[1] = (byte)'g';
            header[2] = (byte)'g';
            header[3] = (byte)'S';
            int keyIndex = 0;
            for (int i = 4; i < header.Length; ++i)
            {
                header[i] ^= key[keyIndex++];
                if (keyIndex >= key.Length)
                    keyIndex = 1;
            }
            return true;
        }
    }
}
