# PpmDecoderSharp

Decode Portable PixMap (ppm) images in .NET8.

## Portable PixMap

| Magic Number | Type | Extension | Image Type | Encoding |
| ------------ | ---- | --------- | ---------- | -------- |
| P1           | PBM  | .pbm      | B/W        | ASCII    |
| P2           | PGM  | .pgm      | Gray       | ASCII    |
| P3           | PPM  | .ppm      | RGB        | ASCII    |
| P4           | PBM  | .pbm      | B/W        | Binary   |
| P5           | PGM  | .pgm      | Gray       | Binary   |
| P6           | PPM  | .ppm      | RGB        | Binary   |

## Usage

```cs
// Read pbm/pgm/ppm image
IPpmImage ppmImage = await PpmImageReader.ReadAsync(@"C:\image.ppm");

// Read raw image
IImage rawImage = await RawImageReader.ReadAsync((@"C:\image.raw", 1920, 1080, 12, 0);
```

## Benchmark [WIP]

> BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.3007/22H2/2022Update/SunValley2)
> AMD Ryzen 7 PRO 4750GE with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
> .NET SDK 8.0.200-preview.23624.5
>   [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2 [AttachedDebugger]
>   DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


| Method     | Filename       |        Mean |      Error |    StdDev |      Median |     Gen0 |     Gen1 |     Gen2 | Allocated |
| ---------- | -------------- | ----------: | ---------: | --------: | ----------: | -------: | -------: | -------: | --------: |
| ReadHeader | P1-200x200.pbm |    78.65 us |   0.508 us |  0.475 us |    78.45 us |   4.0283 |        - |        - |   7.96 KB |
| ReadImage  | P1-200x200.pbm |   459.96 us |   9.005 us | 11.709 us |   455.43 us |  25.3906 |        - |        - |  51.87 KB |
| ReadHeader | P2-300x200.pgm |    75.32 us |   0.255 us |  0.226 us |    75.34 us |   4.0283 |        - |        - |   7.94 KB |
| ReadImage  | P2-300x200.pgm | 1,138.71 us |  14.674 us | 13.726 us | 1,140.11 us |  35.1563 |        - |        - |  75.98 KB |
| ReadHeader | P3-300x300.ppm |    78.10 us |   0.712 us |  0.594 us |    78.21 us |   4.0283 |        - |        - |   8.13 KB |
| ReadImage  | P3-300x300.ppm | 6,935.08 us | 104.674 us | 97.912 us | 6,985.39 us |  78.1250 |  78.1250 |  78.1250 | 321.88 KB |
| ReadHeader | P4-305x400.pbm |    77.23 us |   1.494 us |  1.889 us |    76.07 us |   3.7842 |        - |        - |   7.62 KB |
| ReadImage  | P4-305x400.pbm |   216.83 us |   3.972 us |  3.521 us |   216.33 us |  38.0859 |  38.0859 |  38.0859 | 128.74 KB |
| ReadHeader | P5-300x246.pgm |    78.56 us |   1.184 us |  1.108 us |    78.68 us |   3.7842 |        - |        - |   7.76 KB |
| ReadImage  | P5-300x246.pgm |   123.08 us |   2.279 us |  2.132 us |   122.29 us |  41.5039 |        - |        - |  82.11 KB |
| ReadHeader | P6-640x426.ppm |    79.81 us |   0.578 us |  0.512 us |    79.80 us |   3.7842 |        - |        - |   7.76 KB |
| ReadImage  | P6-640x426.ppm |   537.06 us |  10.726 us | 17.624 us |   535.36 us | 249.0234 | 249.0234 | 249.0234 | 808.97 KB |

## References

[PPMフォーマット | Optical Learning Blog](http://optical-learning-blog.realop.co.jp/?eid=14)

[PNM (画像フォーマット) - Wikipedia](https://ja.wikipedia.org/wiki/PNM_%28%E7%94%BB%E5%83%8F%E3%83%95%E3%82%A9%E3%83%BC%E3%83%9E%E3%83%83%E3%83%88%29)

[Netpbm - Wikipedia](https://en.wikipedia.org/wiki/Netpbm)

[Create 16bit pgm/ppm images - GitHub/hsytkm](https://gist.github.com/hsytkm/3a57b2731a06cede117b768f5bd21f3d)
