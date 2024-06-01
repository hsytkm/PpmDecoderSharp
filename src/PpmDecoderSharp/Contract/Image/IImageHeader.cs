namespace PpmDecoderSharp;

public interface IImageHeader
{
    /// <summary>Image width pixel</summary>
    int Width { get; }

    /// <summary>Image height pixel</summary>
    int Height { get; }

    /// <summary>Maximum pixel value</summary>
    int MaxLevel { get; }

    /// <summary>Gray=1,Color=3</summary>
    int ChannelCount { get; }

    /// <summary>Multiplication result of Channels and Depth.</summary>
    int BitsPerPixel { get; }

    /// <summary>Multiplication result of Channels and Depth.(8bit,3ch -> 3Byte)</summary>
    int BytesPerPixel { get; }

    /// <summary>The number of bytes between two consecutive rows of pixels in an image</summary>
    int Stride { get; }

    /// <summary>Offset from the beginning of the file to pixels.</summary>
    int PixelOffset { get; }

    /// <summary>Allocated size for pixels</summary>
    int PixelsAllocatedSize { get; }
}
