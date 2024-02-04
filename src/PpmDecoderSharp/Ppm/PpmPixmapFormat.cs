namespace PpmDecoderSharp;

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
