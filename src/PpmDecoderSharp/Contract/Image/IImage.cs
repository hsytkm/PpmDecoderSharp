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
    int ChannelCount { get; }

    /// <summary>Multiplication result of Channels and Depth.</summary>
    int BitsPerPixel { get; }

    /// <summary>Multiplication result of Channels and Depth.(8bit,3ch -> 3Byte)</summary>
    int BytesPerPixel { get; }

    /// <summary>The number of bytes between two consecutive rows of pixels in an image</summary>
    int Stride { get; }

    /// <summary>For color images, it is an RGB array</summary>
    ReadOnlySpan<byte> GetRawPixels();

    /// <summary>For color images, it is an RGB array</summary>
    ReadOnlySpan<byte> GetNormalized8bitPixels();

    /// <summary>Save the image to a BMP file</summary>
    void SaveNormalizedBitmapToFile(string? filePath);

    /// <summary>Save the image to a BMP file asynchronously</summary>
    Task SaveNormalizedBitmapToFileAsync(string? filePath, CancellationToken cancellationToken = default);

    /// <summary>For color images, it is an RGB array</summary>
    ReadOnlySpan<byte> GetBitShifted8bitPixels(int bitShift);

    /// <summary>Save the image to a BMP file</summary>
    void SaveBitShiftedBitmapToFile(string? filePath, int bitShift);

    /// <summary>Save the image to a BMP file asynchronously</summary>
    Task SaveBitShiftedBitmapToFileAsync(string? filePath, int bitShift, CancellationToken cancellationToken = default);
}
