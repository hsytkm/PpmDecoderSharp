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

1. MaxLevel 255超 (BigEndian)

#### P3

1. MaxLevel 255超 (BigEndian)

#### P5

1. MaxLevel 255超 (BigEndian)

#### P6

1. MaxLevel 255超 (BigEndian)

#### Common

1. BitShift



## References

[PPMフォーマット | Optical Learning Blog](http://optical-learning-blog.realop.co.jp/?eid=14)

[PNM (画像フォーマット) - Wikipedia](https://ja.wikipedia.org/wiki/PNM_%28%E7%94%BB%E5%83%8F%E3%83%95%E3%82%A9%E3%83%BC%E3%83%9E%E3%83%83%E3%83%88%29)

[Netpbm - Wikipedia](https://en.wikipedia.org/wiki/Netpbm)

