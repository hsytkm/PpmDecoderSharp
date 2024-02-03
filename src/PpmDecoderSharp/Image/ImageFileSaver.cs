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
        // ◆255超は未実装
        ArgumentOutOfRangeException.ThrowIfNotEqual(image.MaxLevel, 255, nameof(image.MaxLevel));

        var bitmap = BitmapImage.CreateBlank(image.Width, image.Height, image.BytesPerPixel * 8, image.Stride, image.GetRawPixels());
        return bitmap.ToMemoryStream();
    }
}
