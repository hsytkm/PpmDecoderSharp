﻿namespace PpmDecoderSharp;

internal enum PpmPixmapFormat
{
    Undefined,

    /// <summary>
    /// PBM  .pbm  B/W   ASCII
    /// </summary>
    P1 = 1,

    /// <summary>
    /// PGM  .pgm  Gray  ASCII
    /// </summary>
    P2 = 2,

    /// <summary>
    /// PPM  .ppm  RGB   ASCII
    /// </summary>
    P3 = 3,

    /// <summary>
    /// PBM  .pbm  B/W   Binary
    /// </summary>
    P4 = 4,

    /// <summary>
    /// PGM  .pgm  Gray  Binary
    /// </summary>
    P5 = 5,

    /// <summary>
    /// PPM  .ppm  RGB   Binary
    /// </summary>
    P6 = 6
}

internal static class PpmPixmapFormatExtension
{
    internal static PpmPixmapFormat GetPixmapFormat(string headerText)
    {
        if (headerText.Length < 2)
            return PpmPixmapFormat.Undefined;

        return headerText.AsSpan()[0..2] switch
        {
            "P1" => PpmPixmapFormat.P1,
            "P2" => PpmPixmapFormat.P2,
            "P3" => PpmPixmapFormat.P3,
            "P4" => PpmPixmapFormat.P4,
            "P5" => PpmPixmapFormat.P5,
            "P6" => PpmPixmapFormat.P6,
            _ => PpmPixmapFormat.Undefined
        };
    }

    internal static int GetChannelCount(this PpmPixmapFormat format) => format switch
    {
        PpmPixmapFormat.P1 or PpmPixmapFormat.P4 => 1,
        PpmPixmapFormat.P2 or PpmPixmapFormat.P5 => 1,
        PpmPixmapFormat.P3 or PpmPixmapFormat.P6 => 3,
        _ => throw new NotSupportedException()
    };
}
