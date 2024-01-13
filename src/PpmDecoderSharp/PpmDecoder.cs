#if false
namespace PpmDecoderSharp;

public interface IPpmDecoder
{
    Task<byte[]> ReadAsync(string filePath);
}

public sealed class PpmDecoder //: IPpmDecoder
{
    public static async Task<PpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        using var stream = File.OpenRead(filePath);
        return await ReadAsync(stream, cancellationToken);
    }

    public static async Task<PpmImage?> ReadAsync(Stream? stream, CancellationToken cancellationToken = default)
    {
        if (stream is null)
            return null;

        var image = await PpmImage.CreateAsync(stream, cancellationToken);
        if (image is null)
            return null;

        return image;
    }

}
#endif
