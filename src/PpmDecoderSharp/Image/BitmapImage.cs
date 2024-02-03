using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PpmDecoderSharp;

// [Windows bitmap - Wikipedia](https://ja.wikipedia.org/wiki/Windows_bitmap)

/// <summary>
/// .bmp file
/// </summary>
internal sealed class BitmapImage
{
    private readonly byte[] _pixels;

    public int Width { get; }
    public int Height { get; }
    public int BytesPerPixel { get; }
    public int Stride { get; }

    public int PixelOffsetBytes { get; }
    public Span<byte> GetPixelsSpan() => _pixels.AsSpan()[PixelOffsetBytes..];

    private BitmapImage(byte[] pixels, in BitmapHeader header)
    {
        _pixels = pixels;
        Width = header.Width;
        Height = header.Height;
        BytesPerPixel = header.BytesPerPixel;
        Stride = header.Stride;
        PixelOffsetBytes = header.OffsetBytes;
    }

    public static BitmapImage Create(int width, int height, int bitsPerPixel, int srcStride, ReadOnlySpan<byte> sourcePixels) => bitsPerPixel switch
    {
        8 => Create1ch(width, height, bitsPerPixel, srcStride, sourcePixels),
        24 => Create3ch(width, height, bitsPerPixel, srcStride, sourcePixels),
        _ => throw new NotSupportedException($"Unsupported bits/pixel = {bitsPerPixel}")
    };

    private static unsafe BitmapImage CreateHeader(int width, int height, int bitsPerPixel)
    {
        bool useColorPalette = bitsPerPixel == 8;    // 8bit gray image must need color palette.
        BitmapHeader bitmapHeader = new(width, height, bitsPerPixel, useColorPalette);
        var pixels = new byte[bitmapHeader.FileSize];

        fixed (byte* headPtr = pixels)
        {
            *(BitmapHeader*)headPtr = bitmapHeader;

            if (useColorPalette)
            {
                uint* ptr = (uint*)(headPtr + bitmapHeader.GetColorPaletteOffsetBytes());
                for (uint i = 0; i < 0x00ff_ffff; i += 0x0001_0101)
                    *(ptr++) = i;
            }
        }
        return new BitmapImage(pixels, bitmapHeader);
    }

    private static unsafe BitmapImage Create1ch(int width, int height, int bitsPerPixel, int srcStride, ReadOnlySpan<byte> sourcePixels)
    {
        const int ch = 1;
        ArgumentOutOfRangeException.ThrowIfNotEqual(bitsPerPixel, 8 * ch);

        var bitmap = CreateHeader(width, height, bitsPerPixel);
        var destStride = bitmap.Stride;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(srcStride, destStride);

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(sourcePixels);
        ref readonly byte destRefBytes = ref MemoryMarshal.AsRef<byte>(bitmap.GetPixelsSpan());

        fixed (byte* srcHead = &srcRefBytes)
        fixed (byte* destHead = &destRefBytes)
        {
            // 画素は左下から右上に向かって記録する仕様
            for (int y = 0; y < height; y++)
            {
                byte* srcRowHead = srcHead + ((height - 1 - y) * srcStride);
                byte* destRowHead = destHead + (y * destStride);
                Unsafe.CopyBlockUnaligned(destRowHead, srcRowHead, (uint)srcStride);
            }
        }
        return bitmap;
    }

    private static unsafe BitmapImage Create3ch(int width, int height, int bitsPerPixel, int srcStride, ReadOnlySpan<byte> sourcePixels)
    {
        const int ch = 3;
        ArgumentOutOfRangeException.ThrowIfNotEqual(bitsPerPixel, 8 * ch);

        var bitmap = CreateHeader(width, height, bitsPerPixel);
        var destStride = bitmap.Stride;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(srcStride, destStride);

        ref readonly byte srcRefBytes = ref MemoryMarshal.AsRef<byte>(sourcePixels);
        ref readonly byte destRefBytes = ref MemoryMarshal.AsRef<byte>(bitmap.GetPixelsSpan());

        fixed (byte* srcHead = &srcRefBytes)
        fixed (byte* destHead = &destRefBytes)
        {
            // 画素は左下から右上に向かって記録する仕様
            for (int y = 0; y < height; y++)
            {
                byte* srcRowHead = srcHead + ((height - 1 - y) * srcStride);
                byte* destRowHead = destHead + (y * destStride);
                byte* srcRowTail = srcRowHead + (width * ch);
                byte* dest = destRowHead;

                // Convert RGB to BGR
                for (byte* src = srcRowHead; src < srcRowTail; src += ch)
                {
                    *(dest++) = *(src + 2);
                    *(dest++) = *(src + 1);
                    *(dest++) = *(src + 0);
                }
            }
        }
        return bitmap;
    }

    internal MemoryStream ToMemoryStream()
    {
        MemoryStream ms = new(_pixels, writable: false);
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
