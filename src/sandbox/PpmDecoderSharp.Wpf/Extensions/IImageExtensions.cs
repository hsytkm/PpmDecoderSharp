using System.Buffers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PpmDecoderSharp.Wpf.Extensions;

public static class IImageExtensions
{
    private const double Dpi = 96.0;

    /// <summary>
    /// Normalize the maximum value of the image to 8 bits.
    /// </summary>
    public static BitmapSource? ToBitmapSourceWithNormalization(this IImage? image, bool isFreeze = true)
    {
        if (image is null)
            return null;

        var image8bit = image.GetNormalized8bitImage();
        return ToBitmapSourceWithBitShift(image8bit, 0, isFreeze);
    }

    /// <summary>
    /// Gets an 8-bit image with the specified bit shift applied.
    /// </summary>
    public static BitmapSource? ToBitmapSourceWithBitShift(this IImage? image, int bitShift = 0, bool isFreeze = true)
    {
        if (image is null)
            return null;

        var pixelsSpan = image.GetBitShifted8bitPixels(bitShift);
        return ToBitmapSourceCore(image, pixelsSpan, isFreeze);
    }

    private static BitmapSource ToBitmapSourceCore(IImage image, ReadOnlySpan<byte> pixelsSpan, bool isFreeze)
    {
        (int width, int height) = (image.Width, image.Height);
        (int stride, int channel) = (image.Stride, image.ChannelCount);

        BitmapSource bitmap = image.MaxLevel switch
        {
            <= 255 => CreateFrom8bit(width, height, stride, channel, pixelsSpan),
            _ => CreateFrom8bit(width, height, stride, channel, pixelsSpan),
            //_ => throw new NotSupportedException($"MaxLv={image.MaxLevel}")
        };

        if (isFreeze)
            bitmap.Freeze();

        return bitmap;
    }

    private static unsafe BitmapSource CreateFrom8bit(int width, int height, int stride, int channel, ReadOnlySpan<byte> pixelsSpan)
    {
        PixelFormat format = channel switch
        {
            1 => PixelFormats.Gray8,
            3 => PixelFormats.Rgb24,    // ppm pixel array is RGB
            _ => throw new NotSupportedException($"Ch={channel}")
        };

#if false
        // Rgb24 を Bgr32 に変換します
        if (format == PixelFormats.Rgb24)
            return ToBgr32FromRgb24(width, height, stride, pixelsSpan);
#endif

        fixed (byte* ptr = pixelsSpan)
        {
            return BitmapSource.Create(width, height, Dpi, Dpi, format, null, (IntPtr)ptr, height * stride, stride);
        }
    }

    private static unsafe BitmapSource ToBgr32FromRgb24(int width, int height, int stride, ReadOnlySpan<byte> pixelsSpan)
    {
        int newStride = width * 4;
        int newSize = height * newStride;

        var newPixels = ArrayPool<byte>.Shared.Rent(newSize);
        try
        {
            fixed (byte* srcHeadPtr = pixelsSpan)
            fixed (byte* destHeadPtr = newPixels)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* srcStPtr = srcHeadPtr + stride * y;
                    byte* srcEdPtr = srcStPtr + width * 3;
                    byte* dest = destHeadPtr + newStride * y;

                    for (byte* src = srcStPtr; src < srcEdPtr; src += 3, dest += 4)
                    {
                        uint bgra = 0xff00_0000 | (((uint)*(src + 0)) << 16) | (((uint)*(src + 1)) << 8) | *(src + 2);
                        *(uint*)dest = bgra;
                    }
                }

                var format = PixelFormats.Bgr32;
                return BitmapSource.Create(width, height, Dpi, Dpi, format, null, (IntPtr)destHeadPtr, height * newStride, newStride);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(newPixels);
        }
    }
}
