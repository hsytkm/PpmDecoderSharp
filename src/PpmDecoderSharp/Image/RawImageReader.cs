using System.Diagnostics;

namespace PpmDecoderSharp;

public static class RawImageReader
{
    /// <summary>Read raw image from stream</summary>
    public static async Task<IImage?> ReadAsync(
        Stream? stream, int width, int height, int rawBits, int stride, int pixelOffset = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rawBits);
        ArgumentOutOfRangeException.ThrowIfLessThan(stride, GetRawBurstStride(width, rawBits));
        ArgumentOutOfRangeException.ThrowIfNegative(pixelOffset);

        int maxLevel = (1 << rawBits) - 1;

        var header = new RawHeader(width, height, maxLevel, rawBits, stride, pixelOffset);
        if (header is null)
            return null;

        byte[] pixels = await PpmReadHelper.ReadAsync(stream, header, cancellationToken);
        return new RawImage(header, pixels);
    }

    /// <summary>Read raw image from stream</summary>
    public static async Task<IImage?> ReadAsync(
        Stream? stream, int width, int height, int rawBits,
        CancellationToken cancellationToken = default)
    {
        const int pixelOffset = 0;
        int stride = GetRawBurstStride(width, rawBits);
        return await ReadAsync(stream, width, height, rawBits, stride, pixelOffset, cancellationToken);
    }

    /// <summary>Read raw image from file</summary>
    public static async Task<IImage?> ReadAsync(
        string? filePath, int width, int height, int rawBits, int stride, int pixelOffset = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            return await ReadAsync(stream, width, height, rawBits, stride, pixelOffset, cancellationToken);
        }
        catch (IOException)
        {
            Debug.WriteLine("The file may be open by another process.");
            return null;
        }
    }

    /// <summary>Read raw image from file</summary>
    public static async Task<IImage?> ReadAsync(
        string? filePath, int width, int height, int rawBits,
        CancellationToken cancellationToken = default)
    {
        const int pixelOffset = 0;
        int stride = GetRawBurstStride(width, rawBits);
        return await ReadAsync(filePath, width, height, rawBits, stride, pixelOffset, cancellationToken);
    }

    private static int GetRawBurstStride(int width, int rawBits)
    {
        int pixelPerBytes = rawBits switch
        {
            <= 8 => 1,
            <= 16 => 2,
            _ => throw new NotSupportedException()
        };
        return width * pixelPerBytes;   // Raw image is 1ch.
    }
}
