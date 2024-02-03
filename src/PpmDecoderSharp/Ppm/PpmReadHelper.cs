using System.Buffers;
using System.Diagnostics;

namespace PpmDecoderSharp;

internal static class PpmReadHelper
{
    private const int PixelTextBufferSize = 4096;

    internal static async Task<byte[]> ReadAsync(Stream stream, PpmHeader header, CancellationToken cancellationToken)
        => await (header.Format switch
        {
            PpmHeader.PixmapFormat.P1 => ReadP1Async(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P2 => ReadP2Async(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P3 => ReadP3Async(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P4 => ReadP4Async(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P5 => ReadP5Async(stream, header, cancellationToken),
            PpmHeader.PixmapFormat.P6 => ReadP6Async(stream, header, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported format : {header.Format}")
        });

    // P1
    private static async Task<byte[]> ReadP1Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format != PpmHeader.PixmapFormat.P1)
            throw new NotSupportedException($"Not supported format : {header.Format}");

        if (header.MaxLevel != 1)
            throw new NotSupportedException($"Not supported max level : {header.MaxLevel}");

        byte[] pixelTextBuffer = ArrayPool<byte>.Shared.Rent(PixelTextBufferSize);
        try
        {
            var pixels = new byte[header.PixelsAllocatedSize];

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

    // P2
    private static async Task<byte[]> ReadP2Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
        => await ReadP3Async(stream, header, cancellationToken);

    // P3
    private static async Task<byte[]> ReadP3Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format is not (PpmHeader.PixmapFormat.P2 or PpmHeader.PixmapFormat.P3))
            throw new NotSupportedException($"Not supported format : {header.Format}");

        byte[] pixelTextBuffer = ArrayPool<byte>.Shared.Rent(PixelTextBufferSize);
        try
        {
            var pixels = new byte[header.PixelsAllocatedSize];

            int pixelWriteIndex = 0;
            bool needCommentCheck = true, isInComment = false;
            int tempValue = -1;     // invalid

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

                    if (b is >= (byte)'0' and <= (byte)'9')
                    {
                        if (tempValue < 0)
                        {
                            tempValue = b - '0';
                        }
                        else
                        {
                            tempValue = (tempValue * 10) + (b - '0');
                        }
                    }
                    else if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                    {
                        if (tempValue >= 0)
                        {
                            if (header.MaxLevel <= 0xff)
                            {
                                pixels[pixelWriteIndex++] = (byte)tempValue;
                            }
                            else if (header.MaxLevel <= 0xffff)
                            {
                                pixels[pixelWriteIndex++] = (byte)(tempValue >> 8);
                                pixels[pixelWriteIndex++] = (byte)(tempValue & 0x00ff);
                            }
                            else
                            {
                                throw new NotSupportedException($"Read value is big. ({tempValue}");
                            }
                            tempValue = -1;     // invalid
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
        }
    }

    // P4
    private static async Task<byte[]> ReadP4Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
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
                throw new NotImplementedException($"Unable to read the intended size. Expected={srcSize}, Actual={readSize}");

            var pixels = new byte[header.PixelsAllocatedSize];
            unsafe
            {
                fixed (byte* destHeadPtr = pixels)
                fixed (byte* srcHeadPtr = buffer)
                {
                    byte* srcTailPtr = srcHeadPtr + (srcStride * imageHeight);
                    byte* dest = destHeadPtr;
                    (int srcWidthMax, int remainder) = Math.DivRem(imageWidth, 8);

                    for (byte* srcRowPtr = srcHeadPtr; srcRowPtr < srcTailPtr; srcRowPtr += srcStride)
                    {
                        byte* srcRowTailPtr = srcRowPtr + srcWidthMax;
                        for (byte* p = srcRowPtr; p < srcRowTailPtr; p++)
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

    // P5
    private static async Task<byte[]> ReadP5Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
        => await ReadP6Async(stream, header, cancellationToken);

    // P6
    private static async Task<byte[]> ReadP6Async(Stream stream, PpmHeader header, CancellationToken cancellationToken)
    {
        if (header.Format is not (PpmHeader.PixmapFormat.P5 or PpmHeader.PixmapFormat.P6))
            throw new NotSupportedException($"Not supported format : {header.Format}");

        var pixels = new byte[header.PixelsAllocatedSize];

        stream.Position = header.PixelOffset;
        int readSize = await stream.ReadAsync(pixels, cancellationToken);
        if (readSize != pixels.Length)
            throw new NotImplementedException($"Unable to read the intended size. Expected={pixels.Length}, Actual={readSize}");

        return pixels;
    }
}
