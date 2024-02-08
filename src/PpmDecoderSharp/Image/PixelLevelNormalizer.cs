using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PpmDecoderSharp;

internal static class PixelLevelNormalizer
{
    internal static byte[] Get8bitPixels(IImageHeader header, byte[] rawPixels) => header.MaxLevel switch
    {
        0xff => rawPixels,
        > 0 and < 0xff => Get8bitPixels_LessThan255(header, rawPixels),
        > 0xff and <= 0xffff => Get8bitPixels_LessThanOrEqual65535(header, rawPixels),
        _ => throw new NotImplementedException($"Not supported format. ({header.MaxLevel})")
    };

    // continuous assumption
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSrcStride(IImageHeader header) => header.Width * header.BytesPerPixel;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDestPixelsSize(IImageHeader header) => header.Height * header.Width * header.Channels;

    private static unsafe byte[] Get8bitPixels_LessThan255(IImageHeader header, byte[] sourcePixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(header.MaxLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(header.MaxLevel, 254);
        double coef = 255d / header.MaxLevel;

        int destPixelsSize = GetDestPixelsSize(header);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourcePixels.Length, destPixelsSize);
        var destPixels = new byte[destPixelsSize];

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(sourcePixels);
        fixed (byte* srcPtr = &srcRefBytes)
        {
            Marshal.Copy((IntPtr)srcPtr, destPixels, 0, destPixelsSize);
        }

        // Level normalization
        fixed (byte* headPtr = destPixels)
        {
            // Memory is assumed to be contiguous.
            byte* tailPtr = headPtr + destPixelsSize;
            for (byte* p = headPtr; p < tailPtr; p++)
            {
                *p = (byte)Math.Min(0xff, (*p * coef) + 0.5);   // round
            }
        }
        return destPixels;
    }

    private static unsafe byte[] Get8bitPixels_LessThanOrEqual65535(IImageHeader header, byte[] sourcePixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(header.MaxLevel, 256);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(header.MaxLevel, 65535);
        double coef = 255d / header.MaxLevel;

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
                *(destPtr++) = (byte)Math.Min(0xff, (value * coef) + 0.5);  // round
            }
        }
        return destPixels;
    }
}
