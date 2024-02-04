using System.Diagnostics;

namespace PpmDecoderSharp;

public static class RawImageReader
{
    /// <summary>Read raw image from file</summary>
    public static async Task<IImage?> ReadAsync(string? filePath, int width, int height, int rawBits, int pixelOffset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            return await ReadAsync(stream, width, height, rawBits, pixelOffset, cancellationToken);
        }
        catch (IOException)
        {
            Debug.WriteLine("The file may be open by another process.");
            return null;
        }
    }

    /// <summary>Read raw image from stream</summary>
    public static async Task<IImage?> ReadAsync(Stream? stream, int width, int height, int rawBits, int pixelOffset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rawBits);
        ArgumentOutOfRangeException.ThrowIfNegative(pixelOffset);

        int maxLevel = (1 << rawBits) - 1;

        // Read raw image using P5(binary 1ch)
        var header = PpmHeader.Create(PpmPixmapFormat.P5, width, height, maxLevel, pixelOffset, null);
        if (header is null)
            return null;

        byte[] pixels = await PpmReadHelper.ReadAsync(stream, header, cancellationToken);
        return new PpmImage(header, pixels);
    }
}
