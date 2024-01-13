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
    string? Comment = null)
{
    private const int HeaderReadSize = 256; // ♪コメントを含んでいたら足りないかも

    public enum PixmapFormat
    {
        Undefined,
        P1,     // PBM  .pbm  binary  Text
        P2,     // PGM  .pgm  gray    Text
        P3,     // PBM  .ppm  rgb     Text
        P4,     // PBM  .pbm  binary  Binary
        P5,     // PGM  .pgm  gray    Binary
        P6      // PBM  .ppm  rgb     Binary
    }

    public int Channels => Format switch
    {
        PixmapFormat.P1 or PixmapFormat.P4 => 1,    // Binary
        PixmapFormat.P2 or PixmapFormat.P5 => 1,    // Gray
        PixmapFormat.P3 or PixmapFormat.P6 => 3,    // RGB
        _ => 0
    };

    public int BytesPerChannel => (MaxLevel / 256) + 1;
    public int BytesPerPixel => Channels * BytesPerChannel;

    /// <summary>Size of pixels only, excluding header</summary>
    public int ImageSize => Width * Height * BytesPerPixel;

    internal static async Task<PpmHeader?> CreateAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] bs = ArrayPool<byte>.Shared.Rent(HeaderReadSize);
        try
        {
            _ = await stream.ReadAsync(bs, cancellationToken);

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
            "P1" => ParseHeaderTextP14(1, headerText),
            "P2" => ParseHeaderTextP2356(2, headerText),
            "P3" => ParseHeaderTextP2356(3, headerText),
            "P4" => ParseHeaderTextP14(4, headerText),
            "P5" => ParseHeaderTextP2356(5, headerText),
            "P6" => ParseHeaderTextP2356(6, headerText),
            _ => null,
        };
    }

    private static PpmHeader? ParseHeaderTextP14(int formatNumber, string headerText)
    {
        PixmapFormat format = formatNumber switch
        {
            1 => PixmapFormat.P1,
            4 => PixmapFormat.P4,
            _ => throw new NotSupportedException($"Not supported format number. ({formatNumber})"),
        };

        var match = PpmHeaderRegex14().Match(headerText);
        if (!match.Success)
        {
            Debug.WriteLine("Regex14 isn't matched.");
            return null;
        }

        var comment = GetHeaderComment(match.Groups["comment"].Value);

        if (!int.TryParse(match.Groups["width"].Value, out int width))
            return null;

        var lastGroup = match.Groups["height"];
        if (!int.TryParse(lastGroup.Value, out int height))
            return null;

        int offset = lastGroup.Index + lastGroup.Length + 1;
        return new(format, width, height, 1, offset, comment);
    }

    private static PpmHeader? ParseHeaderTextP2356(int formatNumber, string headerText)
    {
        PixmapFormat format = formatNumber switch
        {
            2 => PixmapFormat.P2,
            3 => PixmapFormat.P3,
            5 => PixmapFormat.P5,
            6 => PixmapFormat.P6,
            _ => throw new NotSupportedException($"Not supported format number. ({formatNumber})"),
        };

        var match = PpmHeaderRegex2356().Match(headerText);
        if (!match.Success)
        {
            Debug.WriteLine("Regex2356 isn't matched.");
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
        return new(format, width, height, maxLevel, offset, comment);
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
