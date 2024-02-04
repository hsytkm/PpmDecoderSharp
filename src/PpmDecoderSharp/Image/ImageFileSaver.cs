namespace PpmDecoderSharp;

internal static class ImageFileSaver
{
    private const FileMode DefaultMode = FileMode.Create;
    private const FileAccess DefaultAccess = FileAccess.Write;
    private const FileShare DefaultShare = FileShare.None;
    private const int DefaultBufferSize = 4096;

    internal static void SaveToBmp(IImage image, string filePath)
    {
        using MemoryStream ms = GetBitmapStream(image);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize);
        fs.Seek(0, SeekOrigin.Begin);

        ms.CopyTo(fs);
    }

    internal static async Task SaveToBmpAsync(IImage image, string filePath, CancellationToken cancellationToken)
    {
        using MemoryStream ms = GetBitmapStream(image);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize, useAsync: true);
        fs.Seek(0, SeekOrigin.Begin);

        await ms.CopyToAsync(fs, cancellationToken);
    }

    private static MemoryStream GetBitmapStream(IImage image)
    {
        (int width, int height, int channels) = (image.Width, image.Height, image.Channels);
        int bytesPerPixel = channels;   // normalize 8bit
        int bitsPerPixel = bytesPerPixel * 8;
        int stride = width * bytesPerPixel;
        var pixels = image.Get8bitNormalizedPixels();
        var bitmap = BitmapImage.Create(width, height, bitsPerPixel, stride, pixels);
        return bitmap.ToMemoryStream();
    }
}
