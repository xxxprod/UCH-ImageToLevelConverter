using System;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace UCH_ImageToLevelConverter.Tools;

public static class SevenZipHelper
{
    private const int DictionarySize = 8388608;

    private const bool Eos = false;

    private static readonly CoderPropID[] PropIDs = new CoderPropID[8]
    {
        CoderPropID.DictionarySize,
        CoderPropID.PosStateBits,
        CoderPropID.LitContextBits,
        CoderPropID.LitPosBits,
        CoderPropID.Algorithm,
        CoderPropID.NumFastBytes,
        CoderPropID.MatchFinder,
        CoderPropID.EndMarker
    };

    private static readonly object[] Properties = new object[8] {DictionarySize, 2, 3, 0, 2, 128, "bt4", Eos};

    public static byte[] Compress(byte[] inputBytes)
    {
        MemoryStream memoryStream = new(inputBytes);
        MemoryStream memoryStream2 = new();
        Encoder encoder = new();
        encoder.SetCoderProperties(PropIDs, Properties);
        encoder.WriteCoderProperties(memoryStream2);
        long length = memoryStream.Length;
        for (int i = 0; i < 8; i++) memoryStream2.WriteByte((byte) (length >> (8 * i)));
        encoder.Code(memoryStream, memoryStream2, -1L, -1L, null);
        return memoryStream2.ToArray();
    }

    public static byte[] Decompress(byte[] inputBytes)
    {
        MemoryStream memoryStream = new(inputBytes);
        Decoder decoder = new();
        memoryStream.Seek(0L, SeekOrigin.Begin);
        MemoryStream memoryStream2 = new();
        byte[] array = new byte[5];
        if (memoryStream.Read(array, 0, 5) != 5) throw new Exception("input .lzma is too short");
        long num = 0L;
        for (int i = 0; i < 8; i++)
        {
            int num2 = memoryStream.ReadByte();
            if (num2 < 0) throw new Exception("Can't Read 1");
            num |= (long) ((ulong) (byte) num2 << (8 * i));
        }

        decoder.SetDecoderProperties(array);
        long inSize = memoryStream.Length - memoryStream.Position;
        decoder.Code(memoryStream, memoryStream2, inSize, num, null);
        return memoryStream2.ToArray();
    }
}