using System.Buffers;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PpmDecoderSharp.Wpf;

public static class PpmImageExtensions
{
    private const double Dpi = 96.0;

    public static BitmapSource ToBitmapSource(this IImage image, bool isFreeze = true)
    {
        var bitmap = (image.Channels, image.MaxLevel) switch
        {
            // P1/P2/P4/P5
            (1, 0xff) => ToBitmapSource1ByteMax(image, PixelFormats.Gray8),
            (1, < 0xff) => ToBitmapSource1ByteUnderMax(image, PixelFormats.Gray8),
            (1, 0xffff) => ToBitmapSource2ByteMax(image, PixelFormats.Gray8),
            //(1, < 0xffff) => ToBitmapSource2ByteUnderMax(image, PixelFormats.Gray8),

            // P3/P6 (ppm pixel array is RGB)
            (3, 0xff) => ToBitmapSource1ByteMax(image, PixelFormats.Rgb24),
            (3, < 0xff) => ToBitmapSource1ByteUnderMax(image, PixelFormats.Rgb24),
            (3, 0xffff) => ToBitmapSource2ByteMax(image, PixelFormats.Rgb24),
            //(3, < 0xffff) => ToBitmapSource2ByteUnderMax(image, PixelFormats.Rgb24),
            _ => throw new NotImplementedException($"Ch={image.Channels}, MaxLv={image.MaxLevel}")
        };

        if (isFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    // Byte=1/MaxLv=255
    private static unsafe BitmapSource ToBitmapSource1ByteMax(IImage image, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(image.MaxLevel, 255, nameof(image.MaxLevel));

        ref readonly byte refBytes = ref MemoryMarshal.AsRef<byte>(image.GetRawPixels());

        fixed (byte* srcPtr = &refBytes)
        {
            return BitmapSource.Create(
                image.Width, image.Height, Dpi, Dpi, pixelFormat, null,
                (IntPtr)srcPtr, image.Height * image.Stride, image.Stride);
        }
    }

    // Byte=1/MaxLv=1~254
    private static unsafe BitmapSource ToBitmapSource1ByteUnderMax(IImage image, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(image.MaxLevel, 1, nameof(image.MaxLevel));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(image.MaxLevel, 254, nameof(image.MaxLevel));

        float ratio = 255f / image.MaxLevel;
        int pixelSize = image.Stride * image.Height;
        byte[] pixels = ArrayPool<byte>.Shared.Rent(pixelSize);

        try
        {
            ref readonly byte refBytes = ref MemoryMarshal.AsRef<byte>(image.GetRawPixels());

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
                image.Width, image.Height, Dpi, Dpi, pixelFormat, null,
                pixels, image.Stride);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixels);
        }
    }

    // Byte=2/MaxLv=65535(BigEndian)
    private static unsafe BitmapSource ToBitmapSource2ByteMax(IImage image, in PixelFormat pixelFormat)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(image.MaxLevel, 65535, nameof(image.MaxLevel));

        int destBytesPerPixel = image.Channels;   // dest=8bit
        int destStride = image.Width * destBytesPerPixel;
        int destPixelSize = image.Height * destStride;
        byte[] destPixels = ArrayPool<byte>.Shared.Rent(destPixelSize);

        try
        {
            ref readonly byte srcBytes = ref MemoryMarshal.AsRef<byte>(image.GetRawPixels());

            fixed (byte* fixedSrcPtr = &srcBytes)
            fixed (byte* fixedDestPtr = destPixels)
            {
                ushort* srcHeadPtr = (ushort*)fixedSrcPtr;
                ushort* srcTailPtr = (ushort*)(fixedSrcPtr + (image.Height * image.Stride));
                byte* destPtr = fixedDestPtr;

                // Memory is assumed to be contiguous.
                for (ushort* srcPtr = srcHeadPtr; srcPtr < srcTailPtr; srcPtr++)
                {
                    //ushort value = (ushort)(((*srcPtr & 0xff) << 8) | (*srcPtr >> 8));
                    *(destPtr++) = (byte)(*srcPtr & 0x00ff);    // Upper bit
                }
            }

            return BitmapSource.Create(
                image.Width, image.Height, Dpi, Dpi, pixelFormat, null,
                destPixels, destStride);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(destPixels);
        }
    }
}
