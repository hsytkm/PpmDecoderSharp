using System.Buffers;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PpmDecoderSharp;

/// <summary>
/// Image header information written in text.
/// </summary>
internal sealed partial record PpmHeader(
    PpmHeader.PixmapFormat Format,
    int Width,
    int Height,
    int MaxLevel,
    int PixelOffset,
    string? Comment)
    : IPpmHeader
{
    private const int HeaderAllocSize = 512;    // ♪コメントを含んでいたら足りないかも

    internal enum PixmapFormat
    {
        Undefined,
        P1 = 1,     // PBM  .pbm  B/W   ASCII
        P2 = 2,     // PGM  .pgm  Gray  ASCII
        P3 = 3,     // PPM  .ppm  RGB   ASCII
        P4 = 4,     // PBM  .pbm  B/W   Binary
        P5 = 5,     // PGM  .pgm  Gray  Binary
        P6 = 6      // PPM  .ppm  RGB   Binary
    }

    public int Channels => Format switch
    {
        PixmapFormat.P1 or PixmapFormat.P4 => 1,    // B/W
        PixmapFormat.P2 or PixmapFormat.P5 => 1,    // Gray
        PixmapFormat.P3 or PixmapFormat.P6 => 3,    // RGB
        _ => throw new NotSupportedException($"Unsupported format : {Format}")
    };

    /// <summary>Depth</summary>
    public int BitsPerPixel
    {
        // bitは隙間なく詰められる前提としています
        get
        {
            static int ceilingMaxValueToBitsPerChannel(int value)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(value));

                if (value is 0)
                    return 1;

                int bits = 0;
                for (; value > 0; value >>= 1)
                    bits++;
                return bits;
            }
            var bitsPerChannel = ceilingMaxValueToBitsPerChannel(MaxLevel);
            return Channels * bitsPerChannel;
        }
    }

    /// <summary>Depth</summary>
    public int BytesPerPixel
    {
        get
        {
            static int ceilingBitsToByte(int bits)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bits, 0, nameof(bits));

                int bytes = 0;
                for (; bits > 0; bits -= 8)
                    bytes++;
                return bytes;
            }
            return ceilingBitsToByte(BitsPerPixel);
        }
    }

    /// <summary>Size of pixels only, excluding header</summary>
    public int PixelsAllocatedSize => Height * Width * BytesPerPixel;

    private static PpmHeader? Create(PixmapFormat format, int width, int height, int maxLevel, int pixelOffset, string? comment)
    {
        if (format is PixmapFormat.Undefined)
            return null;

        if (width < 1 || height < 1)
            return null;

        if (maxLevel < 1 || 0xffff < maxLevel)
            return null;

        if (pixelOffset <= 0)   // must be positive
            return null;

        return new(format, width, height, maxLevel, pixelOffset, comment);
    }

    internal static async Task<PpmHeader?> CreateAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] bs = ArrayPool<byte>.Shared.Rent(HeaderAllocSize);
        try
        {
            _ = await stream.ReadAsync(bs.AsMemory(0, HeaderAllocSize), cancellationToken);

            string headerText = System.Text.Encoding.ASCII.GetString(bs);
            return ParseHeaderText(headerText);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bs);
        }
    }

    // ♪private化したい。テスト用にinternal
    internal static PpmHeader? ParseHeaderText(string headerText)
    {
        if (headerText.Length < 2)
            return null;

        return headerText.AsSpan()[0..2] switch
        {
            "P1" => ParseHeaderTextWithoutMax(PixmapFormat.P1, headerText),
            "P2" => ParseHeaderTextWithMax(PixmapFormat.P2, headerText),
            "P3" => ParseHeaderTextWithMax(PixmapFormat.P3, headerText),
            "P4" => ParseHeaderTextWithoutMax(PixmapFormat.P4, headerText),
            "P5" => ParseHeaderTextWithMax(PixmapFormat.P5, headerText),
            "P6" => ParseHeaderTextWithMax(PixmapFormat.P6, headerText),
            _ => null,
        };
    }

    private static PpmHeader? ParseHeaderTextWithoutMax(PixmapFormat format, string headerText)
    {
        var match = PpmHeaderRegex14().Match(headerText);
        if (!match.Success)
        {
            Debug.WriteLine($"Regex14 isn't matched. ({headerText})");
            return null;
        }

        var comment = GetHeaderComment(match.Groups["comment"].Value);

        if (!int.TryParse(match.Groups["width"].Value, out int width))
            return null;

        var lastGroup = match.Groups["height"];
        if (!int.TryParse(lastGroup.Value, out int height))
            return null;

        int offset = GetPixelOffset(headerText, lastGroup.Index + lastGroup.Length);
        if (offset < 0)
            return null;

        return Create(format, width, height, 1, offset, comment);
    }

    private static PpmHeader? ParseHeaderTextWithMax(PixmapFormat format, string headerText)
    {
        var match = PpmHeaderRegex2356().Match(headerText);
        if (!match.Success)
        {
            Debug.WriteLine($"Regex2356 isn't matched. ({headerText})");
            return null;
        }

        var comment = GetHeaderComment(match.Groups["comment"].Value);

        if (!int.TryParse(match.Groups["width"].Value, out int width))
            return null;

        if (!int.TryParse(match.Groups["height"].Value, out int height))
            return null;

        var lastGroup = match.Groups["max"];
        if (!int.TryParse(lastGroup.Value, out int maxLevel))
            return null;

        int offset = GetPixelOffset(headerText, lastGroup.Index + lastGroup.Length);
        if (offset < 0)
            return null;

        return Create(format, width, height, maxLevel, offset, comment);
    }

    private static string? GetHeaderComment(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var comment = source.Trim();
        if (comment.AsSpan().StartsWith("# "))
            return comment[2..];

        return comment;
    }

    private static int GetPixelOffset(string headerText, int startOffset)
    {
        // ToDo: 正規表現を改善すれば本関数を削除できると思っています
        var span = headerText.AsSpan();
        if (span.Length <= startOffset)
        {
            //throw new NotSupportedException($"StartOffset is invalid. ({startOffset})");
            return -1;
        }

        // ToDo: 未確認ですが、セパレータが \r\n の場合に、最終のセパレータを \r と判別して先頭画素を \n(0x0a) と処理してしまう気がします。
        for (int i = startOffset; i < span.Length; i++)
        {
            if (span[i] is not ('\r' or '\n' or ' ' or '\t'))
                return i;
        }
        return span.Length;
    }

    [GeneratedRegex(@"^P(?<no>[14])(?<comment>\s*# .*)?\s+(?<width>\d+)\s+(?<height>\d+).*")]
    private static partial Regex PpmHeaderRegex14();

    [GeneratedRegex(@"^P(?<no>[2356])(?<comment>\s*# .*)?\s+(?<width>\d+)\s+(?<height>\d+)\s+(?<max>\d+).*")]
    private static partial Regex PpmHeaderRegex2356();
}
