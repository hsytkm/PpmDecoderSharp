namespace PpmDecoderSharp;

internal static class PpmFileSaver
{
    private const FileMode DefaultMode = FileMode.Create;
    private const FileAccess DefaultAccess = FileAccess.Write;
    private const FileShare DefaultShare = FileShare.None;
    private const int DefaultBufferSize = 4096;

    internal static void SaveToBmp(IPpmImage ppm, string filePath)
    {
        using MemoryStream ms = GetBitmapStream(ppm);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize);
        fs.Seek(0, SeekOrigin.Begin);

        ms.CopyTo(fs);
    }

    internal static async Task SaveToBmpAsync(IPpmImage ppm, string filePath, CancellationToken cancellationToken)
    {
        using MemoryStream ms = GetBitmapStream(ppm);
        using FileStream fs = new(filePath, DefaultMode, DefaultAccess, DefaultShare, DefaultBufferSize, useAsync: true);
        fs.Seek(0, SeekOrigin.Begin);

        await ms.CopyToAsync(fs, cancellationToken);
    }

    private static MemoryStream GetBitmapStream(IPpmImage ppm)
    {
        // ◆255超は未実装
        ArgumentOutOfRangeException.ThrowIfNotEqual(ppm.MaxLevel, 255, nameof(ppm.MaxLevel));

        var bitmap = BitmapImage.CreateBlank(ppm.Width, ppm.Height, ppm.BytesPerPixel * 8, ppm.Stride, ppm.GetRawPixels());
        return bitmap.ToMemoryStream();
    }
}
