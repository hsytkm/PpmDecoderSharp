namespace PpmDecoderSharp;

/// <summary>
/// Ppm format image
/// </summary>
internal sealed class PpmImage : RawImage, IPpmImage
{
    /// <inheritdoc/>
    public int FormatNumber { get; }

    /// <inheritdoc/>
    public string? Comment { get; }

    internal PpmImage(IPpmHeader header, byte[] pixels)
        : base(header, pixels)
    {
        FormatNumber = (int)header.Format;
        Comment = header.Comment;
    }
}
