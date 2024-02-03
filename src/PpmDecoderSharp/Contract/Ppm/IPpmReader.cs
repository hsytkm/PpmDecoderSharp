namespace PpmDecoderSharp;

internal interface IPpmReader
{
    /// <summary>Read pbm/pgm/ppm image from file</summary>
    static abstract Task<IPpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default);

    /// <summary>Read pbm/pgm/ppm image from stream</summary>
    static abstract Task<IPpmImage?> ReadAsync(Stream? stream, CancellationToken cancellationToken = default);
}
