# PpmDecoderSharp

Decode Portable PixMap (ppm) images in C#



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
PpmImage image = await PpmImage.ReadAsync(@"C:\test.ppm");
```



## Not implemented

#### P2

1. MaxLevel 16bit (256~65534, 65535)

#### P3

1. MaxLevel 16bit (256~65534, 65535)

#### P5

1. MaxLevel 16bit (256~65534, 65535)

#### P6

1. MaxLevel 16bit (256~65534, 65535)

#### Common

1. BitShift



## Benchmark [WIP]

> BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.3007/22H2/2022Update/SunValley2)
> AMD Ryzen 7 PRO 4750GE with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
> .NET SDK 8.0.101
>   [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
>   DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


| Method     | Filename       |        Mean |      Error |     StdDev |      Gen0 |     Gen1 |     Gen2 |  Allocated |
| ---------- | -------------- | ----------: | ---------: | ---------: | --------: | -------: | -------: | ---------: |
| ReadHeader | P1-200x200.pbm |    71.50 us |   0.653 us |   0.611 us |    3.9063 |        - |        - |    7.45 KB |
| ReadImage  | P1-200x200.pbm |   344.40 us |   1.408 us |   1.248 us |   24.9023 |        - |        - |   47.25 KB |
| ReadHeader | P2-300x200.pgm |    71.15 us |   0.590 us |   0.552 us |    3.9063 |        - |        - |    7.44 KB |
| ReadImage  | P2-300x200.pgm | 1,872.52 us |  16.324 us |  15.269 us |  933.5938 |        - |        - | 1907.78 KB |
| ReadHeader | P3-300x300.ppm |    71.12 us |   0.569 us |   0.532 us |    3.9063 |        - |        - |    7.63 KB |
| ReadImage  | P3-300x300.ppm | 8,932.06 us | 173.701 us | 162.480 us | 4140.6250 |  78.1250 |  78.1250 |  8706.7 KB |
| ReadHeader | P4-305x400.pbm |    67.67 us |   0.503 us |   0.471 us |    3.6621 |        - |        - |    7.12 KB |
| ReadImage  | P4-305x400.pbm |   175.11 us |   2.313 us |   2.164 us |   38.3301 |  38.3301 |  38.3301 |  127.05 KB |
| ReadHeader | P5-300x246.pgm |    67.22 us |   0.522 us |   0.512 us |    3.6621 |        - |        - |    7.26 KB |
| ReadImage  | P5-300x246.pgm |    97.29 us |   0.953 us |   0.891 us |   41.5039 |        - |        - |   80.07 KB |
| ReadHeader | P6-640x426.ppm |    71.80 us |   1.073 us |   1.004 us |    3.6621 |        - |        - |    7.26 KB |
| ReadImage  | P6-640x426.ppm |   454.42 us |   5.810 us |   5.435 us |  249.0234 | 249.0234 | 249.0234 |  807.02 KB |

## References

[PPMフォーマット | Optical Learning Blog](http://optical-learning-blog.realop.co.jp/?eid=14)

[PNM (画像フォーマット) - Wikipedia](https://ja.wikipedia.org/wiki/PNM_%28%E7%94%BB%E5%83%8F%E3%83%95%E3%82%A9%E3%83%BC%E3%83%9E%E3%83%83%E3%83%88%29)

[Netpbm - Wikipedia](https://en.wikipedia.org/wiki/Netpbm)

