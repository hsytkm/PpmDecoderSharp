using System.Diagnostics;

namespace PpmDecoderSharp;

/// <summary>
/// Ppm format image
/// </summary>
public sealed record PpmImage : IPpmImage, IPpmReader
{
    private readonly PpmHeader _header;
    private readonly byte[] _pixels;

    /// <inheritdoc/>
    public int Width => _header.Width;

    /// <inheritdoc/>
    public int Height => _header.Height;

    /// <inheritdoc/>
    public int MaxLevel => _header.MaxLevel;

    /// <inheritdoc/>
    public int Channels => _header.Channels;

    /// <inheritdoc/>
    public int BitsPerPixel => _header.BitsPerPixel;

    /// <inheritdoc/>
    public int BytesPerPixel => _header.BytesPerPixel;

    /// <inheritdoc/>
    public int Stride => _header.Width * _header.BytesPerPixel;

    /// <inheritdoc/>
    public int FormatNumber => (int)_header.Format;

    /// <inheritdoc/>
    public string? Comment => _header.Comment;

    private PpmImage(PpmHeader header, byte[] pixels) => (_header, _pixels) = (header, pixels);

    /// <inheritdoc/>
    public ReadOnlySpan<byte> GetRawPixels() => _pixels.AsSpan();

    /// <inheritdoc/>
    public void SaveToBmp(string? filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        ImageFileSaver.SaveToBmp(this, filePath);
    }

    /// <inheritdoc/>
    public async Task SaveToBmpAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        await ImageFileSaver.SaveToBmpAsync(this, filePath, cancellationToken);
    }

    /// <inheritdoc/>
    public static async Task<IPpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

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

    /// <inheritdoc/>
    public static async Task<IPpmImage?> ReadAsync(Stream? stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var header = await PpmHeader.CreateAsync(stream, cancellationToken);
        if (header is null)
            return null;

        byte[] pixels = await PpmReadHelper.ReadAsync(stream, header, cancellationToken);
        return new PpmImage(header, pixels);
    }
}
