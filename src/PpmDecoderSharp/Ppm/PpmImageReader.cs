using System.Diagnostics;

namespace PpmDecoderSharp;

public static class PpmImageReader
{
    /// <summary>Check supported extension</summary>
    public static bool IsSupportedExtension(string? filePath) =>
        Path.GetExtension(filePath)?.ToLowerInvariant() switch
        {
            ".pbm" or ".pgm" or ".ppm" => true,
            _ => false
        };

    /// <summary>Read pbm/pgm/ppm image from file</summary>
    public static async Task<IPpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            return await ReadAsync(stream, cancellationToken);
        }
        catch (IOException)
        {
            Debug.WriteLine("The file may be open by another process.");
            return null;
        }
    }

    /// <summary>Read pbm/pgm/ppm image from stream</summary>
    public static async Task<IPpmImage?> ReadAsync(Stream? stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = await PpmHeaderUtil.CreateAsync(stream, cancellationToken);
        if (header is null)
            return null;

        byte[] pixels = await PpmReadHelper.ReadAsync(stream, header, cancellationToken);
        return new PpmImage(header, pixels);
    }
}
