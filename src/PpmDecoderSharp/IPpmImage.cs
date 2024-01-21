namespace PpmDecoderSharp;

public interface IPpmImage
{
    int FormatNumber { get; }

    int Width { get; }
    int Height { get; }
    int MaxLevel { get; }

    int Channels { get; }
    int BytesPerChannel { get; }
    int BytesPerPixel { get; }
    int Stride { get; }

    string? Comment { get; }

    ReadOnlySpan<byte> AsSpan();
}

internal interface IPpmReader
{
    static abstract Task<IPpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default);
    static abstract Task<IPpmImage?> ReadAsync(Stream? stream, CancellationToken cancellationToken = default);
}
