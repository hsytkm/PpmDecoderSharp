namespace PpmDecoderSharp;

/// <summary>
/// Ppm format image
/// </summary>
public sealed record PpmImage : IPpmImage
{
    private readonly IPpmHeader _header;
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

    internal PpmImage(IPpmHeader header, byte[] pixels) => (_header, _pixels) = (header, pixels);

    /// <inheritdoc/>
    public ReadOnlySpan<byte> GetRawPixels() => _pixels.AsSpan();

    /// <inheritdoc/>
    public ReadOnlySpan<byte> GetNormalized8bitPixels() => PixelLevelNormalizer.Get8bitPixels(_header, _pixels);

    /// <inheritdoc/>
    public void SaveNormalizedBitmapToFile(string? filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        ImageFileSaver.SaveNormalizedBitmapToFile(this, filePath);
    }

    /// <inheritdoc/>
    public async Task SaveNormalizedBitmapToFileAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        await ImageFileSaver.SaveNormalizedBitmapToFileAsync(this, filePath, cancellationToken);
    }

    /// <inheritdoc/>
    public ReadOnlySpan<byte> GetBitShifted8bitPixels(int bitShift) => PixelLevelShifter.Get8bitPixels(_header, _pixels, bitShift);

    /// <inheritdoc/>
    public void SaveBitShiftedBitmapToFile(string? filePath, int bitShift)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        ImageFileSaver.SaveBitShiftedBitmapToFile(this, filePath, bitShift);
    }

    /// <inheritdoc/>
    public async Task SaveBitShiftedBitmapToFileAsync(string? filePath, int bitShift, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (File.Exists(filePath))
            throw new IOException(nameof(filePath));

        await ImageFileSaver.SaveBitShiftedBitmapToFileAsync(this, filePath, bitShift, cancellationToken);
    }
}
