namespace PpmDecoderSharp;

public interface IPpmHeader : IImageHeader
{
    string? Comment { get; }
}
