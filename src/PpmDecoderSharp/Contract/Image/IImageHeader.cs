namespace PpmDecoderSharp;

public interface IImageHeader
{
    int Width { get; }
    int Height { get; }

    int MaxLevel { get; }

    int Channels { get; }
    int BitsPerPixel { get; }
    int BytesPerPixel { get; }

    int PixelOffset { get; }
    int PixelsAllocatedSize { get; }
}
