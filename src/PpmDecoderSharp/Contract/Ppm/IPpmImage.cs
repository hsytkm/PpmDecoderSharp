namespace PpmDecoderSharp;

public interface IPpmImage : IImage
{
    /// <summary>Magic number in ppm format(1~6)</summary>
    int FormatNumber { get; }

    /// <summary>Comment in header</summary>
    string? Comment { get; }
}
