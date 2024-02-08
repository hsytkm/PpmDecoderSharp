namespace PpmDecoderSharp;

public interface IPpmHeader : IImageHeader
{
    /// <summary>Magic number in ppm format(P1~)</summary>
    internal PpmPixmapFormat Format { get; }

    /// <summary>Comment in ppm header</summary>
    string? Comment { get; }
}
