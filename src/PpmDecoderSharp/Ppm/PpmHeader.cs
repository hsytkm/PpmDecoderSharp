namespace PpmDecoderSharp;

/// <summary>
/// Image header information written in text.
/// </summary>
internal sealed record PpmHeader : RawHeader, IPpmHeader
{
    /// <inheritdoc />
    public PpmPixmapFormat Format { get; }

    /// <inheritdoc />
    public string? Comment { get; }

    internal PpmHeader(PpmPixmapFormat format, int width, int height, int maxLevel, int pixelOffset, string? comment)
        : base(width, height, format.GetChannelCount(), maxLevel, GetPixelPerBits(format, maxLevel), GetStride(format, width, maxLevel), pixelOffset)
    {
        (Format, Comment) = (format, comment);
    }

    private static int GetPixelPerBits(PpmPixmapFormat format, int maxLevel)
    {
        if (format is PpmPixmapFormat.P1 or PpmPixmapFormat.P4)
            return 1;

        return GetBitCount(maxLevel) * format.GetChannelCount();

        static int GetBitCount(int value)
        {
            if (value is 0)
                return 1;

            int count = 0;
            while (value > 0)
            {
                value >>= 1;
                count++;
            }
            return count;
        }
    }

    private static int GetPixelPerBytes(PpmPixmapFormat format, int maxLevel)
    {
        return (int)Math.Ceiling(GetPixelPerBits(format, maxLevel) / 8f);
    }

    private static int GetStride(PpmPixmapFormat format, int width, int maxLevel)
    {
        return width * GetPixelPerBytes(format, maxLevel);
    }
}
