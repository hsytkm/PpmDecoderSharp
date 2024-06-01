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
    int Channels { get; }

    /// <summary>Multiplication result of Channels and Depth.</summary>
    int BitsPerPixel { get; }

    /// <summary>Multiplication result of Channels and Depth.(8bit,3ch -> 3Byte)</summary>
    int BytesPerPixel { get; }

    /// <summary>Offset from the beginning of the file to pixels.</summary>
    int PixelOffset { get; }

    /// <summary>Bytes from the start of one row of pixels to the start of the next row.</summary>
    int Stride { get; }

    /// <summary>Allocated size for pixels</summary>
    int PixelsAllocatedSize { get; }
}
