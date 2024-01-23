using System.Runtime.InteropServices;

namespace PpmDecoderSharp;

// [Windows bitmap - Wikipedia](https://ja.wikipedia.org/wiki/Windows_bitmap)

internal sealed class BitmapImage
{
    private readonly byte[] _bs;

    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public int BytesPerPixel { get; }
    public int PixelOffsetBytes { get; }

    public Span<byte> GetPixelsSpan() => _bs.AsSpan()[PixelOffsetBytes..];

    private BitmapImage(byte[] bs, in BitmapHeader header)
    {
        _bs = bs;
        Width = header.Width;
        Height = header.Height;
        BytesPerPixel = header.BytesPerPixel;
        Stride = header.Stride;
        PixelOffsetBytes = header.OffsetBytes;
    }

    internal static unsafe BitmapImage CreateBlank(int width, int height, int bitsPerPixel)
    {
        bool useColorPalette = bitsPerPixel == 8;    // 8bit gray image must need this.
        BitmapHeader bitmapHeader = new(width, height, bitsPerPixel, useColorPalette);
        var buffer = new byte[bitmapHeader.FileSize];

        fixed (byte* headPtr = buffer)
        {
            *(BitmapHeader*)headPtr = bitmapHeader;

            if (useColorPalette)
            {
                byte* ptr = headPtr + bitmapHeader.GetColorPaletteOffsetBytes();
                for (int i = 0; i < 256; i++)
                {
                    *(ptr++) = (byte)i;
                    *(ptr++) = (byte)i;
                    *(ptr++) = (byte)i;
                    *(ptr++) = 0;
                }
            }
        }
        return new BitmapImage(buffer, bitmapHeader);
    }

    internal MemoryStream ToMemoryStream()
    {
        MemoryStream ms = new(_bs, writable: false);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 54)]
    private readonly struct BitmapHeader
    {
#pragma warning disable IDE0049
        // Bitmap File Header
        public readonly Int16 FileType;
        public readonly Int32 FileSize;
        public readonly Int16 Reserved1;
        public readonly Int16 Reserved2;
        public readonly Int32 OffsetBytes;

        // Bitmap Information Header
        public readonly Int32 InfoSize;
        public readonly Int32 Width;
        public readonly Int32 Height;
        public readonly Int16 Planes;
        public readonly Int16 BitCount;
        public readonly Int32 Compression;
        public readonly Int32 SizeImage;
        public readonly Int32 XPixPerMete;
        public readonly Int32 YPixPerMete;
        public readonly Int32 ClrUsed;
        public readonly Int32 CirImportant;

        // consts
        private const Int16 FileHeaderValue = 0x4d42;   // 'B','M'
        private const Int32 PixelPerMeter = 3780;       // pixel/meter (96dpi / 2.54cm * 100m)
        private const Int32 FileHeaderSize = 14;
        private const Int32 InfoHeaderSize = 40;
        private const Int32 ColorPaletteLevel = 256;
        private const Int32 ColorPalettesSize = ColorPaletteLevel * 4;
#pragma warning restore IDE0049

        internal BitmapHeader(int width, int height, int bitsPerPixel, bool useColorPalette)
        {
            var headerSize = FileHeaderSize + InfoHeaderSize + (useColorPalette ? ColorPalettesSize : 0);
            var imageSize = GetImageSize(width, height, bitsPerPixel);

            FileType = FileHeaderValue;
            FileSize = headerSize + imageSize;
            Reserved1 = 0;
            Reserved2 = 0;
            OffsetBytes = headerSize;

            InfoSize = InfoHeaderSize;
            Width = width;
            Height = height;
            Planes = 1;
            BitCount = bitsPerPixel switch
            {
                8 => 8,
                24 => 24,
                _ => throw new NotSupportedException($"BitsPerPixel={bitsPerPixel}")
            };
            Compression = 0;
            SizeImage = 0;      // 無圧縮の場合、ファイルサイズでなく 0 を設定するらしい
            XPixPerMete = PixelPerMeter;
            YPixPerMete = PixelPerMeter;
            ClrUsed = useColorPalette ? ColorPaletteLevel : 0;
            CirImportant = 0;
        }

        private static int BitsToBytes(int bits) => (bits + (8 - 1)) / 8;

        public int BytesPerPixel => BitsToBytes(BitCount);

        public int Stride => GetStride(Width, BitCount);

        private static int GetStride(int width, int bitsPerPixel)
        {
            var bytesPerPixel = BitsToBytes(bitsPerPixel);
            return (width * bytesPerPixel + (4 - 1)) / 4 * 4;   // multiple of 4
        }

        private static int GetImageSize(int width, int height, int bitsPerPixel)
            => GetStride(width, bitsPerPixel) * height;

        internal int GetColorPaletteOffsetBytes() => ClrUsed is 0 ? -1 : OffsetBytes - ColorPalettesSize;
    }
}
