namespace PpmDecoderSharp;

internal /*sealed*/ record RawHeader : IImageHeader
{
    /// <inheritdoc />
    public int Width { get; }

    /// <inheritdoc />
    public int Height { get; }

    /// <inheritdoc />
    public int MaxLevel { get; }

    /// <inheritdoc />
    public int ChannelCount { get; }

    /// <inheritdoc />
    public int PixelBits { get; }

    /// <inheritdoc />
    public int Stride { get; }

    /// <inheritdoc />
    public int PixelOffset { get; }

    internal RawHeader(int width, int height, int channel, int maxLevel, int pixelBits, int stride, int pixelOffset)
    {
        (Width, Height, ChannelCount) = (width, height, channel);
        (MaxLevel, PixelBits, Stride, PixelOffset) = (maxLevel, pixelBits, stride, pixelOffset);
    }

    /// <inheritdoc />
    public int BitsPerPixel
    {
        // bitは隙間なく詰められる前提としています
        get
        {
            static int ceilingMaxValueToBitsPerChannel(int value)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);

                if (value is 0)
                    return 1;

                int bits = 0;
                for (; value > 0; value >>= 1)
                    bits++;
                return bits;
            }
            var bitsPerChannel = ceilingMaxValueToBitsPerChannel(MaxLevel);
            return ChannelCount * bitsPerChannel;
        }
    }

    /// <inheritdoc />
    public int BytesPerPixel
    {
        get
        {
            static int ceilingBitsToByte(int bits)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bits, 0);

                int bytes = 0;
                for (; bits > 0; bits -= 8)
                    bytes++;
                return bytes;
            }
            return ceilingBitsToByte(BitsPerPixel);
        }
    }

    /// <inheritdoc />
    public int PixelsAllocatedSize => Height * Stride;
}
