using System.Buffers;

namespace PpmDecoderSharp;

public sealed record PpmImage
{
    private readonly PpmHeader _header;
    private readonly byte[] _pixels;

    public int FormatNumber => (int)_header.Format;
    public int Width => _header.Width;
    public int Height => _header.Height;
    public int Channels => _header.Channels;
    public int BytesPerChannel => _header.BytesPerChannel;
    public int BytesPerPixel => _header.BytesPerPixel;
    public int Stride => _header.Width * _header.BytesPerPixel;
    public int MaxLevel => _header.MaxLevel;
    public string? Comment => _header.Comment;

    public ReadOnlySpan<byte> AsSpan() => _pixels.AsSpan();

    private PpmImage(PpmHeader header, byte[] pixels) => (_header, _pixels) = (header, pixels);

    public static async Task<PpmImage?> ReadAsync(string? filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        using var stream = File.OpenRead(filePath);
        return await ReadAsync(stream, cancellationToken);
    }

    public static async Task<PpmImage?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var header = await PpmHeader.CreateAsync(stream, cancellationToken);
        if (header is null)
            return null;

        var readPixelsTask = header.Format switch
        {
            PpmHeader.PixmapFormat.P1 => throw new NotImplementedException("P1"),
            PpmHeader.PixmapFormat.P2 => throw new NotImplementedException("P2"),
            PpmHeader.PixmapFormat.P3 => throw new NotImplementedException("P3"),
            PpmHeader.PixmapFormat.P4 => ReadBinaryPixelsAsync(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P5 or
            PpmHeader.PixmapFormat.P6 => ReadLevelPixelsAsync(stream, header, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported Format : {header.Format}")
        };
        byte[] pixels = await readPixelsTask;

        return new PpmImage(header, pixels);
    }

    // P4
    private static async Task<byte[]> ReadBinaryPixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.MaxLevel != 1)
            throw new NotSupportedException($"Not supported max level: {header.MaxLevel}");

        int dstPixelSize = header.ImageSize;
        int srcPixelSize = (dstPixelSize + (8 - 1)) / 8;    // Ceiling
        byte[] buffer = ArrayPool<byte>.Shared.Rent(srcPixelSize);

        try
        {
            stream.Position = header.PixelOffset;
            int readSize = await stream.ReadAsync(buffer.AsMemory(0, srcPixelSize), cancellationToken);
            if (readSize != srcPixelSize)
                throw new NotImplementedException($"Unable to load the intended size. Expected={srcPixelSize}, Actual={readSize}");

            var pixels = new byte[dstPixelSize];

            unsafe
            {
                fixed (byte* srcHeadPtr = buffer)
                fixed (byte* destHeadPtr = pixels)
                {
                    byte* srcTailPtr = srcHeadPtr + srcPixelSize;
                    byte* dest = destHeadPtr;
                    for (byte* p = srcHeadPtr; p < srcTailPtr; p++)
                    {
                        *(dest++) = ((*p & 0x80) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x40) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x20) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x10) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x08) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x04) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x02) is 0) ? (byte)0 : (byte)1;
                        *(dest++) = ((*p & 0x01) is 0) ? (byte)0 : (byte)1;
                    }
                }
            }
            return pixels;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // P5/P6
    private static async Task<byte[]> ReadLevelPixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.MaxLevel > 255)
            throw new NotSupportedException($"Not supported max level: {header.MaxLevel}");

        int pixelSize = header.ImageSize;
        var pixels = new byte[pixelSize];

        stream.Position = header.PixelOffset;
        int readSize = await stream.ReadAsync(pixels, cancellationToken);
        if (readSize != pixelSize)
            throw new NotImplementedException($"Unable to load the intended size. Expected={pixelSize}, Actual={readSize}");

        return pixels;
    }
}
