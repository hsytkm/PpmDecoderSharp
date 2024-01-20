using System.Buffers;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PpmDecoderSharp.Wpf;

public static class PpmImageExtensions
{
    private const double Dpi = 96.0;

    public static BitmapSource ToBitmapSource(this PpmImage ppm, bool isFreeze = true)
    {
        var bitmap = (ppm.Channels, ppm.BytesPerChannel, ppm.MaxLevel) switch
        {
            (1, 1, 0xff) => ToBitmapSource1ByteMax(ppm, PixelFormats.Gray8),
            (1, 1, < 0xff) => ToBitmapSource1ByteUnderMax(ppm, PixelFormats.Gray8),
            (3, 1, 0xff) => ToBitmapSource1ByteMax(ppm, PixelFormats.Rgb24),
            (3, 1, < 0xff) => ToBitmapSource1ByteUnderMax(ppm, PixelFormats.Rgb24),
            _ => throw new NotImplementedException($"Ch={ppm.Channels}, Bytes/Ch={ppm.BytesPerChannel}, MaxLv={ppm.MaxLevel}")
        };

        if (isFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    // Byte=1/MaxLv=255
    private static unsafe BitmapSource ToBitmapSource1ByteMax(PpmImage ppm, in PixelFormat pixelFormat)
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
    private static unsafe BitmapSource ToBitmapSource1ByteUnderMax(PpmImage ppm, in PixelFormat pixelFormat)
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

}
