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
IPpmImage image = await PpmImage.ReadAsync(@"C:\test.ppm");
```



## Not implemented

#### P2

1. MaxLevel 16bit (256~65534)

#### P3

1. MaxLevel 16bit (256~65534)

#### P5

1. MaxLevel 16bit (256~65534)

#### P6

1. MaxLevel 16bit (256~65534)

#### Common

1. BitShift



## Benchmark [WIP]

> BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.3007/22H2/2022Update/SunValley2)
> AMD Ryzen 7 PRO 4750GE with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
> .NET SDK 8.0.101
>   [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
>   DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2


| Method     | Filename       | Mean        | Error      | StdDev     | Gen0     | Gen1     | Gen2     | Allocated |
|----------- |--------------- |------------:|-----------:|-----------:|---------:|---------:|---------:|----------:|
| ReadHeader | P1-200x200.pbm |    79.32 μs |   0.675 μs |   0.631 μs |   4.0283 |        - |        - |   7.96 KB |
| ReadImage  | P1-200x200.pbm |   468.76 μs |   9.125 μs |  12.181 μs |  25.3906 |        - |        - |  51.54 KB |
| ReadHeader | P2-300x200.pgm |    80.59 μs |   0.760 μs |   0.711 μs |   4.0283 |        - |        - |   7.94 KB |
| ReadImage  | P2-300x200.pgm | 1,282.31 μs |  20.662 μs |  19.327 μs |  35.1563 |        - |        - |  75.31 KB |
| ReadHeader | P3-300x300.ppm |    79.29 μs |   1.367 μs |   1.278 μs |   4.0283 |        - |        - |   8.13 KB |
| ReadImage  | P3-300x300.ppm | 8,038.53 μs | 156.604 μs | 192.324 μs |  78.1250 |  78.1250 |  78.1250 |  321.5 KB |
| ReadHeader | P4-305x400.pbm |    75.70 μs |   1.463 μs |   1.369 μs |   3.7842 |        - |        - |   7.62 KB |
| ReadImage  | P4-305x400.pbm |   215.10 μs |   2.898 μs |   2.711 μs |  38.0859 |  38.0859 |  38.0859 |  128.4 KB |
| ReadHeader | P5-300x246.pgm |    77.97 μs |   1.108 μs |   1.036 μs |   3.7842 |        - |        - |   7.76 KB |
| ReadImage  | P5-300x246.pgm |   116.54 μs |   2.077 μs |   1.943 μs |  41.5039 |        - |        - |  81.42 KB |
| ReadHeader | P6-640x426.ppm |    74.71 μs |   0.248 μs |   0.207 μs |   3.7842 |        - |        - |   7.76 KB |
| ReadImage  | P6-640x426.ppm |   517.72 μs |   7.719 μs |   7.927 μs | 249.0234 | 249.0234 | 249.0234 | 808.58 KB |

## References

[PPMフォーマット | Optical Learning Blog](http://optical-learning-blog.realop.co.jp/?eid=14)

[PNM (画像フォーマット) - Wikipedia](https://ja.wikipedia.org/wiki/PNM_%28%E7%94%BB%E5%83%8F%E3%83%95%E3%82%A9%E3%83%BC%E3%83%9E%E3%83%83%E3%83%88%29)

[Netpbm - Wikipedia](https://en.wikipedia.org/wiki/Netpbm)

