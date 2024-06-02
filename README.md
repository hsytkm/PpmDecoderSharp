# PpmDecoderSharp

Decode Portable PixMap images in .NET8.



## About PixMap

**Header format**

```
P<x>
# comment
<width> <height>
<max>
```

**Example**

```
P6
640 480
65535
```

**Kinds**

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
(int width, int height, int pixelBits) = (640, 480, 12);
IImage rawImage1 = await RawImageReader.ReadAsync(@"C:\image1.raw", width, height, pixelBits);

// Read raw image with stride/Offset
(int stride, int pixelOffset) = (643, 0x10);
IImage rawImage2 = await RawImageReader.ReadAsync(@"C:\image2.raw", width, height, pixelBits, stride, pixelOffset);
```



## Benchmark

> BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.3672/22H2/2022Update/SunValley2)
> AMD Ryzen 7 PRO 4750GE with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
> .NET SDK 8.0.300
>   [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2 [AttachedDebugger]
>   DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2


| Method     | Filename       |        Mean |     Error |    StdDev |     Gen0 |     Gen1 |     Gen2 | Allocated |
| ---------- | -------------- | ----------: | --------: | --------: | -------: | -------: | -------: | --------: |
| ReadHeader | P1-200x200.pbm |    69.76 us |  0.303 us |  0.283 us |   3.9063 |        - |        - |   7.46 KB |
| ReadImage  | P1-200x200.pbm |   345.65 us |  4.595 us |  4.073 us |  24.9023 |        - |        - |  47.38 KB |
| ReadHeader | P2-300x200.pgm |    72.01 us |  1.324 us |  1.238 us |   3.9063 |        - |        - |   7.44 KB |
| ReadImage  | P2-300x200.pgm |   842.29 us |  4.745 us |  4.439 us |  34.1797 |        - |        - |  67.02 KB |
| ReadHeader | P3-300x300.ppm |    69.91 us |  0.333 us |  0.295 us |   3.9063 |        - |        - |   7.63 KB |
| ReadImage  | P3-300x300.ppm | 5,275.37 us | 16.544 us | 15.475 us |  78.1250 |  78.1250 |  78.1250 | 272.26 KB |
| ReadHeader | P4-305x400.pbm |    73.66 us |  0.300 us |  0.266 us |   3.6621 |        - |        - |   7.12 KB |
| ReadImage  | P4-305x400.pbm |   194.32 us |  1.287 us |  1.141 us |  38.3301 |  38.3301 |  38.3301 | 127.17 KB |
| ReadHeader | P5-300x246.pgm |    69.91 us |  1.326 us |  1.419 us |   3.6621 |        - |        - |   7.26 KB |
| ReadImage  | P5-300x246.pgm |   101.39 us |  0.581 us |  0.543 us |  41.5039 |        - |        - |   80.3 KB |
| ReadHeader | P6-640x426.ppm |    72.04 us |  0.450 us |  0.399 us |   3.6621 |        - |        - |   7.26 KB |
| ReadImage  | P6-640x426.ppm |   455.97 us |  3.875 us |  3.625 us | 249.5117 | 249.5117 | 249.5117 | 807.15 KB |



## References

[PPMフォーマット | Optical Learning Blog](http://optical-learning-blog.realop.co.jp/?eid=14)

[PNM (画像フォーマット) - Wikipedia](https://ja.wikipedia.org/wiki/PNM_%28%E7%94%BB%E5%83%8F%E3%83%95%E3%82%A9%E3%83%BC%E3%83%9E%E3%83%83%E3%83%88%29)

[Netpbm - Wikipedia](https://en.wikipedia.org/wiki/Netpbm)

[Create 16bit pgm/ppm images - GitHub/hsytkm](https://gist.github.com/hsytkm/3a57b2731a06cede117b768f5bd21f3d)
