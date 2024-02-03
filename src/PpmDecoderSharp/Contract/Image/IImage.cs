namespace PpmDecoderSharp;

public interface IImage
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

    /// <summary>The number of bytes between two consecutive rows of pixels in an image</summary>
    int Stride { get; }

    /// <summary>Color images are in RGB array</summary>
    ReadOnlySpan<byte> GetRawPixels();

    /// <summary>Color images are in RGB array</summary>
    ReadOnlySpan<byte> Get8bitNormalizedPixels();

    /// <summary>Save the image to a BMP file</summary>
    void SaveToBmp(string? filePath);

    /// <summary>Save the image to a BMP file asynchronously</summary>
    Task SaveToBmpAsync(string? filePath, CancellationToken cancellationToken = default);
}
