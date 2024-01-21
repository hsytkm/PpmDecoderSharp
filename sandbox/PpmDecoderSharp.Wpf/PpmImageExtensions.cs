using System.Buffers;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PpmDecoderSharp.Wpf;

public static class PpmImageExtensions
{
    private const double Dpi = 96.0;

    public static BitmapSource ToBitmapSource(this IPpmImage ppm, bool isFreeze = true)
    {
        var bitmap = (ppm.Channels, ppm.BytesPerChannel, ppm.MaxLevel) switch
        {
            // P1/P2/P4/P5
            (1, 1, 0xff) => ToBitmapSource1ByteMax(ppm, PixelFormats.Gray8),
            (1, 1, < 0xff) => ToBitmapSource1ByteUnderMax(ppm, PixelFormats.Gray8),
            (1, 2, 0xffff) => ToBitmapSource2ByteMax(ppm, PixelFormats.Gray8),
            //(1, 2, < 0xffff) => ToBitmapSource2ByteUnderMax(ppm, PixelFormats.Gray8),

            // P3/P6
            (3, 1, 0xff) => ToBitmapSource1ByteMax(ppm, PixelFormats.Rgb24),
            (3, 1, < 0xff) => ToBitmapSource1ByteUnderMax(ppm, PixelFormats.Rgb24),
            (3, 2, 0xffff) => ToBitmapSource2ByteMax(ppm, PixelFormats.Rgb24),
            //(3, 2, < 0xffff) => ToBitmapSource2ByteUnderMax(ppm, PixelFormats.Rgb24),
            _ => throw new NotImplementedException($"Ch={ppm.Channels}, Bytes/Ch={ppm.BytesPerChannel}, MaxLv={ppm.MaxLevel}")
        };

        if (isFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    // Byte=1/MaxLv=255
    private static unsafe BitmapSource ToBitmapSource1ByteMax(IPpmImage ppm, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.MaxLevel, 255, nameof(ppm.MaxLevel));

        ref readonly byte refBytes = ref MemoryMarshal.AsRef<byte>(ppm.AsSpan());

        fixed (byte* srcPtr = &refBytes)
        {
            return BitmapSource.Create(
                ppm.Width, ppm.Height, Dpi, Dpi, pixelFormat, null,
                (IntPtr)srcPtr, ppm.Height * ppm.Stride, ppm.Stride);
        }
    }

    // Byte=1/MaxLv=1~254
    private static unsafe BitmapSource ToBitmapSource1ByteUnderMax(IPpmImage ppm, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ppm.MaxLevel, 1, nameof(ppm.MaxLevel));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ppm.MaxLevel, 254, nameof(ppm.MaxLevel));

        float ratio = 255f / ppm.MaxLevel;
        int pixelSize = ppm.Stride * ppm.Height;
        byte[] pixels = ArrayPool<byte>.Shared.Rent(pixelSize);

        try
        {
            ref readonly byte refBytes = ref MemoryMarshal.AsRef<byte>(ppm.AsSpan());

            fixed (byte* srcPtr = &refBytes)
            {
                Marshal.Copy((IntPtr)srcPtr, pixels, 0, pixelSize);
            }

            // Level normalization
            fixed (byte* headPtr = pixels)
            {
                // Memory is assumed to be contiguous.
                byte* tailPtr = headPtr + pixelSize;
                for (byte* p = headPtr; p < tailPtr; p++)
                {
                    *p = (byte)Math.Min(byte.MaxValue, (*p * ratio) + 0.5f);    // round
                }
            }

            return BitmapSource.Create(
                ppm.Width, ppm.Height, Dpi, Dpi, pixelFormat, null,
                pixels, ppm.Stride);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixels);
        }
    }

    // Byte=2/MaxLv=65535(BigEndian)
    private static unsafe BitmapSource ToBitmapSource2ByteMax(IPpmImage ppm, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.MaxLevel, 65535, nameof(ppm.MaxLevel));

        int destBytesPerPixel = ppm.Channels;   // dest=8bit
        int destStride = ppm.Width * destBytesPerPixel;
        int destPixelSize = ppm.Height * destStride;
        byte[] destPixels = ArrayPool<byte>.Shared.Rent(destPixelSize);

        try
        {
            ref readonly byte srcBytes = ref MemoryMarshal.AsRef<byte>(ppm.AsSpan());

            fixed (byte* fixedSrcPtr = &srcBytes)
            fixed (byte* fixedDestPtr = destPixels)
            {
                ushort* srcHeadPtr = (ushort*)fixedSrcPtr;
                ushort* srcTailPtr = (ushort*)(fixedSrcPtr + (ppm.Height * ppm.Stride));
                byte* destPtr = fixedDestPtr;

                // Memory is assumed to be contiguous.
                for (ushort* srcPtr = srcHeadPtr; srcPtr < srcTailPtr; srcPtr++)
                {
                    //ushort value = (ushort)(((*srcPtr & 0xff) << 8) | (*srcPtr >> 8));
                    *(destPtr++) = (byte)(*srcPtr & 0x00ff);    // Upper bit
                }
            }

            return BitmapSource.Create(
                ppm.Width, ppm.Height, Dpi, Dpi, pixelFormat, null,
                destPixels, destStride);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(destPixels);
        }
    }
}
