using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PpmDecoderSharp;

internal static class PixelLevelShifter
{
    internal static byte[] Get8bitPixels(IImageHeader header, byte[] rawPixels, int bitShift) => header.MaxLevel switch
    {
        > 0 and <= 0xff => Get8bitPixels_1Byte(header, rawPixels, bitShift),
        > 0xff and <= 0xffff => Get8bitPixels_2Byte(header, rawPixels, bitShift),
        _ => throw new NotImplementedException($"Unsupported format. ({header.MaxLevel})")
    };

    // continuous assumption
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSrcStride(IImageHeader header) => header.Width * header.BytesPerPixel;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDestPixelsSize(IImageHeader header) => header.Height * header.Width * header.Channels;

    private static unsafe byte[] Get8bitPixels_1Byte(IImageHeader header, byte[] sourcePixels, int bitShift)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(header.MaxLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(header.MaxLevel, 255);

        int destPixelsSize = GetDestPixelsSize(header);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourcePixels.Length, destPixelsSize);
        var destPixels = new byte[destPixelsSize];

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(sourcePixels);
        fixed (byte* srcPtr = &srcRefBytes)
        {
            Marshal.Copy((IntPtr)srcPtr, destPixels, 0, destPixelsSize);
        }

        if (bitShift is 0)
            return destPixels;

        fixed (byte* headPtr = destPixels)
        {
            // Memory is assumed to be contiguous.
            byte* tailPtr = headPtr + destPixelsSize;
            for (byte* p = headPtr; p < tailPtr; p++)
            {
                *p = bitShift switch
                {
                    > 0 => (byte)Math.Min(0xff, *p << bitShift),
                    < 0 => (byte)(*p >> (-bitShift)),
                    _ => *p     // just in case
                };
            }
        }
        return destPixels;
    }

    private static unsafe byte[] Get8bitPixels_2Byte(IImageHeader header, byte[] sourcePixels, int bitShift)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(header.MaxLevel, 256);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(header.MaxLevel, 65535);

        int destPixelsSize = GetDestPixelsSize(header);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourcePixels.Length, destPixelsSize);
        var destPixels = new byte[destPixelsSize];

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(sourcePixels);
        (int srcHeight, int srcStride) = (header.Height, GetSrcStride(header));

        fixed (byte* fixedSrcPtr = &srcRefBytes)
        fixed (byte* fixedDestPtr = destPixels)
        {
            ushort* srcHeadPtr = (ushort*)fixedSrcPtr;
            ushort* srcTailPtr = (ushort*)(fixedSrcPtr + (srcHeight * srcStride));
            byte* destPtr = fixedDestPtr;

            // Memory is assumed to be contiguous.
            for (ushort* srcPtr = srcHeadPtr; srcPtr < srcTailPtr; srcPtr++)
            {
                ushort value = BinaryPrimitives.ReadUInt16BigEndian(new ReadOnlySpan<byte>(srcPtr, sizeof(ushort)));
                *(destPtr++) = bitShift switch
                {
                    > 0 => (byte)Math.Min(0xff, (uint)value << bitShift),   // for overflow
                    < 0 => (byte)Math.Min(0xff, value >> (-bitShift)),
                    _ => (byte)Math.Min((ushort)0xff, value)
                };
            }
        }
        return destPixels;
    }
}
