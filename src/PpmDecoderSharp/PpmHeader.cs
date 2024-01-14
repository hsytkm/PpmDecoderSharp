using System.Buffers;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PpmDecoderSharp;

internal sealed partial record PpmHeader(
    PpmHeader.PixmapFormat Format,
    int Width,
    int Height,
    int MaxLevel,
    int PixelOffset,
    string? Comment)
{
    private const int HeaderAllocSize = 512;    // ♪コメントを含んでいたら足りないかも

    internal enum PixmapFormat
    {
        Undefined,
        P1,     // PBM  .pbm  B/W   ASCII
        P2,     // PGM  .pgm  Gray  ASCII
        P3,     // PPM  .ppm  RGB   ASCII
        P4,     // PBM  .pbm  B/W   Binary
        P5,     // PGM  .pgm  Gray  Binary
        P6      // PPM  .ppm  RGB   Binary
    }

    public int Channels => Format switch
    {
        PixmapFormat.P1 or PixmapFormat.P4 => 1,    // B/W
        PixmapFormat.P2 or PixmapFormat.P5 => 1,    // Gray
        PixmapFormat.P3 or PixmapFormat.P6 => 3,    // RGB
        _ => throw new NotSupportedException($"Not supported format : {Format}")
    };

    public int BytesPerChannel => (MaxLevel / 256) + 1;
    public int BytesPerPixel => Channels * BytesPerChannel;

    /// <summary>Size of pixels only, excluding header</summary>
    public int ImageSize => Width * Height * BytesPerPixel;

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

        int offset = lastGroup.Index + lastGroup.Length + 1;
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

        int offset = lastGroup.Index + lastGroup.Length + 1;
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

    [GeneratedRegex(@"^P(?<no>[14])(?<comment>\s*# .*)?\s+(?<width>\d+)\s+(?<height>\d+).*")]
    private static partial Regex PpmHeaderRegex14();

    [GeneratedRegex(@"^P(?<no>[2356])(?<comment>\s*# .*)?\s+(?<width>\d+)\s+(?<height>\d+)\s+(?<max>\d+).*")]
    private static partial Regex PpmHeaderRegex2356();
}
