using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

BenchmarkDotNet.Running.BenchmarkRunner.Run<DecodePpm>();

public class DecodeConfig : ManualConfig
{
    public DecodeConfig()
    {
        AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
        AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
        //AddJob(BenchmarkDotNet.Jobs.Job.ShortRun);
    }
}

[Config(typeof(DecodeConfig))]
public class DecodePpm
{
    private const string BasePath = @"Assets/";

    [Params(
        "P1-200x200.pbm",
        "P2-300x200.pgm",
        "P3-300x300.ppm",
        "P4-305x400.pbm",
        "P5-300x246.pgm",
        "P6-640x426.ppm")]
    public string Filename = default!;

    private string? _imageFilePath;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _imageFilePath = Path.Combine(BasePath, Filename);
    }

    [Benchmark]
    public async ValueTask ReadHeader()
    {
        using var stream = File.OpenRead(_imageFilePath!);
        _ = await PpmDecoderSharp.PpmHeaderUtil.CreateAsync(stream, default);
    }

    [Benchmark]
    public async ValueTask ReadImage()
    {
        _ = await PpmDecoderSharp.PpmImageReader.ReadAsync(_imageFilePath);
    }

    //[GlobalCleanup] public void GlobalCleanup() { }
}
