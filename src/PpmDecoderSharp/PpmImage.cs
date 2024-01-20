using System.Buffers;
using System.Diagnostics;

namespace PpmDecoderSharp;

/// <summary>
/// Ppm format image
/// </summary>
public sealed record PpmImage
{
    private const int PixelTextBufferSize = 4096;

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

        try
        {
            using var stream = File.OpenRead(filePath);
            return await ReadAsync(stream, cancellationToken);
        }
        catch (IOException)
        {
            Debug.WriteLine("The file may be open by another process.");
            return null;
        }
    }

    public static async Task<PpmImage?> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var header = await PpmHeader.CreateAsync(stream, cancellationToken);
        if (header is null)
            return null;

        byte[] pixels = await (header.Format switch
        {
            PpmHeader.PixmapFormat.P1 => ReadBinaryTextPixelsAsync(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P2 or
            PpmHeader.PixmapFormat.P3 => ReadValueTextPixelsAsync(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P4 => ReadBinaryPixelsAsync(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P5 or
            PpmHeader.PixmapFormat.P6 => ReadValuePixelsAsync(stream, header, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported Format : {header.Format}")
        });

        return new PpmImage(header, pixels);
    }

    // P1
    private static async Task<byte[]> ReadBinaryTextPixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format != PpmHeader.PixmapFormat.P1)
            throw new NotSupportedException($"Not supported format : {header.Format}");

        if (header.MaxLevel != 1)
            throw new NotSupportedException($"Not supported max level : {header.MaxLevel}");

        int pixelSize = header.ImageSize;
        byte[] pixelTextBuffer = ArrayPool<byte>.Shared.Rent(PixelTextBufferSize);
        var pixels = new byte[pixelSize];
        try
        {
            int writeIndex = 0;
            bool needCommentCheck = true, isInComment = false;

            int readCount;
            stream.Position = header.PixelOffset;
            while ((readCount = await stream.ReadAsync(pixelTextBuffer, cancellationToken)) != 0)
            {
                for (int i = 0; i < readCount; i++)
                {
                    byte b = pixelTextBuffer[i];

                    // Skip comment in pixel text.
                    if (b is (byte)'\r' or (byte)'\n')
                    {
                        needCommentCheck = true;
                        isInComment = false;
                    }
                    else
                    {
                        if (needCommentCheck && b is (byte)'#')
                            isInComment = true;

                        needCommentCheck = false;
                    }

                    if (isInComment)
                        continue;

                    if (b is (byte)'0')
                    {
                            pixels[writeIndex++] = 0;
                    }
                    else if (b is (byte)'1')
                    {
                            pixels[writeIndex++] = 1;
                    }
                    else if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                    {
                        // skip
                    }
                    else
                    {
                            Debug.WriteLine($"Ignore text : {(char)b} (0x{b:X2})");
                    }
                }
            }
            return pixels;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixelTextBuffer);
        }
    }

    // P2/P3
    private static async Task<byte[]> ReadValueTextPixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format is not (PpmHeader.PixmapFormat.P2 or PpmHeader.PixmapFormat.P3))
            throw new NotSupportedException($"Not supported format : {header.Format}");

        if (header.MaxLevel > 255)
            throw new NotSupportedException($"Not supported max level : {header.MaxLevel}");

        int pixelSize = header.ImageSize;
        byte[] pixelTextBuffer = ArrayPool<byte>.Shared.Rent(PixelTextBufferSize);
        byte[] wordTextBuffer = ArrayPool<byte>.Shared.Rent(16);   // Even 2 Bytes depth can fit in 5 Bytes.
        var pixels = new byte[pixelSize];
        try
        {
            int pixelWriteIndex = 0, wordTextWriteIndex = 0;
            bool needCommentCheck = true, isInComment = false;

            int readCount;
            stream.Position = header.PixelOffset;
            while ((readCount = await stream.ReadAsync(pixelTextBuffer, cancellationToken)) != 0)
            {
                for (int i = 0; i < readCount; i++)
                {
                    byte b = pixelTextBuffer[i];

                    // Skip comment in pixel text.
                    if (b is (byte)'\r' or (byte)'\n')
                    {
                        needCommentCheck = true;
                        isInComment = false;
                    }
                    else
                    {
                        if (needCommentCheck && b is (byte)'#')
                            isInComment = true;

                        needCommentCheck = false;
                    }

                    if (isInComment)
                        continue;

                    if ((byte)'0' <= b && b <= (byte)'9')
                    {
                        wordTextBuffer[wordTextWriteIndex++] = b;
                    }
                    else if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                    {
                        if (wordTextWriteIndex > 0)
                        {
                            var text = System.Text.Encoding.ASCII.GetString(wordTextBuffer.AsSpan()[0..wordTextWriteIndex]);
                            wordTextWriteIndex = 0;

                            if (int.TryParse(text, out int value))
                            {
                                pixels[pixelWriteIndex++] = value switch
                                {
                                    >= 0 and <= 255 => (byte)value,
                                    _ => throw new NotSupportedException($"Value must be between 0 and 255 ({value})"),
                                };
                            }
                            else
                            {
                                throw new NotSupportedException($"Must be numeric value. ({text})");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Ignore text : {(char)b} (0x{b:X2})");
                    }
                }
            }
            return pixels;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixelTextBuffer);
            ArrayPool<byte>.Shared.Return(wordTextBuffer);
        }
    }

    // P4
    private static async Task<byte[]> ReadBinaryPixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format != PpmHeader.PixmapFormat.P4)
            throw new NotSupportedException($"Not supported format : {header.Format}");

        if (header.MaxLevel != 1)
            throw new NotSupportedException($"Not supported max level : {header.MaxLevel}");

        (int imageWidth, int imageHeight) = (header.Width, header.Height);
        int srcStride = (imageWidth + (8 - 1)) / 8;   // Ceiling
        int srcSize = imageHeight * srcStride;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(srcSize);
        try
        {
            stream.Position = header.PixelOffset;
            int readSize = await stream.ReadAsync(buffer.AsMemory(0, srcSize), cancellationToken);
            if (readSize != srcSize)
                throw new NotImplementedException($"Unable to load the intended size. Expected={srcSize}, Actual={readSize}");

            var pixels = new byte[header.ImageSize];
            unsafe
            {
                fixed (byte* destHeadPtr = pixels)
                fixed (byte* srcHeadPtr = buffer)
                {
                    byte* dest = destHeadPtr;
                    byte* srcRowHeadPtr = srcHeadPtr;
                    (int srcWidthMax, int remainder) = Math.DivRem(imageWidth, 8);

                    for (int y = 0; y < imageHeight; y++)
                    {
                        byte* srcRowTailPtr = srcRowHeadPtr + srcWidthMax;

                        for (byte* p = srcRowHeadPtr; p < srcRowTailPtr; p++)
                        {
                            *((ulong*)dest) =
                                  (((*p & 0x80) is 0) ? 0UL : 0x0000_0000_0000_0001)
                                | (((*p & 0x40) is 0) ? 0UL : 0x0000_0000_0000_0100)
                                | (((*p & 0x20) is 0) ? 0UL : 0x0000_0000_0001_0000)
                                | (((*p & 0x10) is 0) ? 0UL : 0x0000_0000_0100_0000)
                                | (((*p & 0x08) is 0) ? 0UL : 0x0000_0001_0000_0000)
                                | (((*p & 0x04) is 0) ? 0UL : 0x0000_0100_0000_0000)
                                | (((*p & 0x02) is 0) ? 0UL : 0x0001_0000_0000_0000)
                                | (((*p & 0x01) is 0) ? 0UL : 0x0100_0000_0000_0000);

                            dest += sizeof(ulong);
                        }

                        if (remainder > 0)
                        {
                            const byte start = 0x80;
                            byte end = (byte)(start >> remainder);

                            for (int mask = start; mask > end; mask >>= 1)
                                *(dest++) = ((*srcRowTailPtr & mask) is 0) ? (byte)0 : (byte)1;
                        }

                        srcRowHeadPtr += srcStride;
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
    private static async Task<byte[]> ReadValuePixelsAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format is not (PpmHeader.PixmapFormat.P5 or PpmHeader.PixmapFormat.P6))
            throw new NotSupportedException($"Not supported format : {header.Format}");

        if (header.MaxLevel > 255)
            throw new NotSupportedException($"Not supported max level : {header.MaxLevel}");

        int pixelSize = header.ImageSize;
        var pixels = new byte[pixelSize];

        stream.Position = header.PixelOffset;
        int readSize = await stream.ReadAsync(pixels, cancellationToken);
        if (readSize != pixelSize)
            throw new NotImplementedException($"Unable to load the intended size. Expected={pixelSize}, Actual={readSize}");

        return pixels;
    }

}
