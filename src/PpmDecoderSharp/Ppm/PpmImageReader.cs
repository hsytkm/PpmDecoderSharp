using System.Diagnostics;

namespace PpmDecoderSharp;

public static class PpmImageReader
{
    /// <summary>Read pbm/pgm/ppm image from file</summary>
    public static async Task<IPpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

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

        var header = await PpmHeader.CreateAsync(stream, cancellationToken);
        if (header is null)
            return null;

        byte[] pixels = await PpmReadHelper.ReadAsync(stream, header, cancellationToken);
        return new PpmImage(header, pixels);
    }
}
