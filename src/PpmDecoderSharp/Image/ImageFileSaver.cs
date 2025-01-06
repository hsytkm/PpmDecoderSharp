namespace PpmDecoderSharp;

internal static class ImageFileSaver
{
    private const FileMode DefaultMode = FileMode.Create;
    private const FileAccess DefaultAccess = FileAccess.Write;
    private const FileShare DefaultShare = FileShare.None;
    private const int DefaultBufferSize = 4096;

    internal static void SaveNormalizedBitmapToFile(IImage image, string filePath)
    {
        using MemoryStream ms = GetNormalizedBitmapStream(image);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize);
        fs.Seek(0, SeekOrigin.Begin);

        ms.CopyTo(fs);
    }

    internal static async Task SaveNormalizedBitmapToFileAsync(IImage image, string filePath, CancellationToken cancellationToken)
    {
        using MemoryStream ms = GetNormalizedBitmapStream(image);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize, useAsync: true);
        fs.Seek(0, SeekOrigin.Begin);

        await ms.CopyToAsync(fs, cancellationToken);
    }

    private static MemoryStream GetNormalizedBitmapStream(IImage image)
    {
        (int width, int height) = (image.Width, image.Height);
        int bitsPerPixel = image.BytesPerPixel * 8;
        int stride = image.Stride;
        var pixels = image.GetNormalized8bitPixels();
        var bitmap = BitmapImage.Create(width, height, bitsPerPixel, stride, pixels);
        return bitmap.ToMemoryStream();
    }

    internal static void SaveBitShiftedBitmapToFile(IImage image, string filePath, int bitShift)
    {
        using MemoryStream ms = GetBitShiftedBitmapStream(image, bitShift);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize);
        fs.Seek(0, SeekOrigin.Begin);

        ms.CopyTo(fs);
    }

    internal static async Task SaveBitShiftedBitmapToFileAsync(IImage image, string filePath, int bitShift, CancellationToken cancellationToken)
    {
        using MemoryStream ms = GetBitShiftedBitmapStream(image, bitShift);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize, useAsync: true);
        fs.Seek(0, SeekOrigin.Begin);

        await ms.CopyToAsync(fs, cancellationToken);
    }

    private static MemoryStream GetBitShiftedBitmapStream(IImage image, int bitShift)
    {
        (int width, int height) = (image.Width, image.Height);
        int bitsPerPixel = image.BytesPerPixel * 8;
        int stride = image.Stride;
        var pixels = image.GetBitShifted8bitPixels(bitShift);
        var bitmap = BitmapImage.Create(width, height, bitsPerPixel, stride, pixels);
        return bitmap.ToMemoryStream();
    }
}
