using System.Runtime.InteropServices;
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

        var pixelsSpan = image.GetNormalized8bitPixels();
        return ToBitmapSource(image, pixelsSpan, isFreeze);
    }

    /// <summary>
    /// Gets an 8-bit image with the specified bit shift applied.
    /// </summary>
    public static BitmapSource? ToBitmapSourceWithBitShift(this IImage? image, int bitShift = 0, bool isFreeze = true)
    {
        if (image is null)
            return null;

        var pixelsSpan = image.GetBitShifted8bitPixels(bitShift);
        return ToBitmapSource(image, pixelsSpan, isFreeze);
    }

    private static unsafe BitmapSource ToBitmapSource(IImage image, ReadOnlySpan<byte> pixelsSpan, bool isFreeze)
    {
        (int imageWidth, int imageHeight, int imageStride) = (image.Width, image.Height, image.Stride);
        var pixelFormat = image.ChannelCount switch
        {
            1 => PixelFormats.Gray8,
            3 => PixelFormats.Rgb24,    // ppm pixel array is RGB
            _ => throw new NotSupportedException($"Ch={image.ChannelCount}")
        };

        fixed (byte* ptr = pixelsSpan)
        {
            var bitmap = BitmapSource.Create(
                imageWidth, imageHeight, Dpi, Dpi, pixelFormat, null,
                (IntPtr)ptr, imageHeight * imageStride, imageStride);

            if (isFreeze)
                bitmap.Freeze();

            return bitmap;
        }
    }
}
