﻿using System.Buffers;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PpmDecoderSharp;

internal static partial class PpmHeaderUtil
{
    private const int HeaderAllocSize = 512;    // ♪コメントを含んでいたら足りないかも

    internal static PpmHeader? Create(PpmPixmapFormat format, int width, int height, int maxLevel, int pixelOffset, string? comment)
    {
        if (format is PpmPixmapFormat.Undefined)
            return null;

        if (width < 1 || height < 1)
            return null;

        if (maxLevel < 1 || 0xffff < maxLevel)
            return null;

        if (pixelOffset < 0)    // Allow 0 for raw images
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
        var format = PpmPixmapFormatExtension.GetPixmapFormat(headerText);
        return format switch
        {
            PpmPixmapFormat.P1 or PpmPixmapFormat.P4 => ParseHeaderTextWithoutMax(format, headerText),
            PpmPixmapFormat.P2 or PpmPixmapFormat.P5 or
            PpmPixmapFormat.P3 or PpmPixmapFormat.P6 => ParseHeaderTextWithMax(format, headerText),
            _ => null
        };
    }

    private static PpmHeader? ParseHeaderTextWithoutMax(PpmPixmapFormat format, string headerText)
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

    private static PpmHeader? ParseHeaderTextWithMax(PpmPixmapFormat format, string headerText)
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
