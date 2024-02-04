namespace PpmDecoderSharp;

public interface IPpmHeader : IImageHeader
{
    internal PpmPixmapFormat Format { get; }
    string? Comment { get; }
}
