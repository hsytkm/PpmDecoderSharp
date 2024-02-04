using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PpmDecoderSharp.Wpf;

public static class PpmImageExtensions
{
    private const double Dpi = 96.0;

    public static BitmapSource ToBitmapSourceWithNormalization(this IImage image, bool isFreeze = true)
    {
        var pixelsSpan = image.Get8bitNormalizedPixels();
        return ToBitmapSource(image, pixelsSpan, isFreeze);
    }

    public static BitmapSource ToBitmapSourceWithBitShift(this IImage image, int bitShift, bool isFreeze = true)
    {
        var pixelsSpan = image.Get8bitPixels(bitShift);
        return ToBitmapSource(image, pixelsSpan, isFreeze);
    }

    private static unsafe BitmapSource ToBitmapSource(IImage image, ReadOnlySpan<byte> pixelsSpan, bool isFreeze)
    {
        (int imageWidth, int imageHeight, int imageChannels) = (image.Width, image.Height, image.Channels);
        int stride = imageWidth * imageChannels;   // 8bit
        var pixelFormat = imageChannels switch
        {
            1 => PixelFormats.Gray8,
            3 => PixelFormats.Rgb24,    // ppm pixel array is RGB
            _ => throw new NotSupportedException($"Ch={imageChannels}")
        };

        ref readonly byte refPixels = ref MemoryMarshal.AsRef<byte>(pixelsSpan);
        fixed (byte* ptr = &refPixels)
        {
            var bitmap = BitmapSource.Create(
                imageWidth, imageHeight, Dpi, Dpi, pixelFormat, null,
                (IntPtr)ptr, imageHeight * stride, stride);

            if (isFreeze)
                bitmap.Freeze();

            return bitmap;
        }
    }
}
