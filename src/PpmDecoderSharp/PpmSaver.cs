using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PpmDecoderSharp;

internal static class PpmSaver
{
    internal static void SaveToBmp(IPpmImage ppm, string filePath)
    {
        using MemoryStream ms = GetBitmapStream(ppm);
        using FileStream fs = new(filePath, FileMode.Create);
        fs.Seek(0, SeekOrigin.Begin);

        ms.CopyTo(fs);
    }

    internal static async Task SaveToBmpAsync(IPpmImage ppm, string filePath, CancellationToken cancellationToken)
    {
        using MemoryStream ms = GetBitmapStream(ppm);
        using FileStream fs = new(filePath, FileMode.Create);
        fs.Seek(0, SeekOrigin.Begin);

        await ms.CopyToAsync(fs, cancellationToken);
    }

    private static MemoryStream GetBitmapStream(IPpmImage ppm) => ppm.Channels switch
    {
        1 => GetGrayBitmapStream(ppm),
        3 => GetColorBitmapStream(ppm),
        _ => throw new NotSupportedException($"Unsupported format : P{ppm.FormatNumber}")
    };

    private static unsafe MemoryStream GetGrayBitmapStream(IPpmImage ppm)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.Channels, 1, nameof(ppm.Channels));
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.MaxLevel, 255, nameof(ppm.MaxLevel));

        (var width, var height) = (ppm.Width, ppm.Height);
        var srcStride = ppm.Stride;
        var bitmap = BitmapImage.CreateBlank(width, height, ppm.BytesPerPixel * 8);
        var destStride = bitmap.Stride;
        Debug.Assert(srcStride <= destStride);

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(ppm.AsSpan());
        ref readonly byte destRefBytes = ref MemoryMarshal.AsRef<byte>(bitmap.GetPixelsSpan());

        fixed (byte* srcHead = &srcRefBytes)
        fixed (byte* destHead = &destRefBytes)
        {
            // 画素は左下から右上に向かって記録する仕様
            for (int y = 0; y < height; y++)
            {
                byte* srcRowHead = srcHead + ((height - 1 - y) * srcStride);
                byte* destRowHead = destHead + (y * destStride);
                Unsafe.CopyBlockUnaligned(destRowHead, srcRowHead, (uint)srcStride);
            }
        }
        return bitmap.ToMemoryStream();
    }

    private static unsafe MemoryStream GetColorBitmapStream(IPpmImage ppm)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.Channels, 3, nameof(ppm.Channels));
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.MaxLevel, 255, nameof(ppm.MaxLevel));

        (var width, var height) = (ppm.Width, ppm.Height);
        var srcStride = ppm.Stride;
        var bitmap = BitmapImage.CreateBlank(width, height, ppm.BytesPerPixel * 8);
        var destStride = bitmap.Stride;
        Debug.Assert(srcStride <= destStride);

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(ppm.AsSpan());
        ref readonly byte destRefBytes = ref MemoryMarshal.AsRef<byte>(bitmap.GetPixelsSpan());

        fixed (byte* srcHead = &srcRefBytes)
        fixed (byte* destHead = &destRefBytes)
        {
            // 画素は左下から右上に向かって記録する仕様
            for (int y = 0; y < height; y++)
            {
                byte* srcRowHead = srcHead + ((height - 1 - y) * srcStride);
                byte* destRowHead = destHead + (y * destStride);
                byte* srcRowTail = srcRowHead + (width * 3);
                byte* dest = destRowHead;

                // Convert RGB to BGR
                for (byte* src = srcRowHead; src < srcRowTail; src += 3)
                {
                    *(dest++) = *(src + 2);
                    *(dest++) = *(src + 1);
                    *(dest++) = *(src + 0);
                }
            }
        }
        return bitmap.ToMemoryStream();
    }
}
