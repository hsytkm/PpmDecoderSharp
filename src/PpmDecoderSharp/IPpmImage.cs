namespace PpmDecoderSharp;

public interface IPpmImage
{
    /// <summary>Magic number in ppm format(1~6)</summary>
    int FormatNumber { get; }

    /// <summary>Image width pixel</summary>
    int Width { get; }

    /// <summary>Image height pixel</summary>
    int Height { get; }

    /// <summary>Maximum pixel value</summary>
    int MaxLevel { get; }

    /// <summary>Comments written in the header</summary>
    int Channels { get; }

    /// <summary>Depth(P1/P4=1Byte, else=1or2Byte)</summary>
    int BytesPerChannel { get; }

    /// <summary>Multiplication result of Channels and Depth. (8bit,3ch -> 3Byte)</summary>
    int BytesPerPixel { get; }

    /// <summary>The number of bytes between two consecutive rows of pixels in an image</summary>
    int Stride { get; }

    /// <summary>Comment in header</summary>
    string? Comment { get; }

    /// <summary>Color images are in RGB array</summary>
    ReadOnlySpan<byte> GetRawPixels();

    /// <summary>Save the image to a BMP file</summary>
    void SaveToBmp(string? filePath);

    /// <summary>Save the image to a BMP file asynchronously</summary>
    Task SaveToBmpAsync(string? filePath, CancellationToken cancellationToken = default);
}
