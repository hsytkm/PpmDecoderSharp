using System.Diagnostics;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PpmDecoderSharp.Wpf;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _imageFilePath = "input_ppm_file_path";

    [ObservableProperty]
    private PpmProperty? _ppmProp;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveImageCommand))]
    private BitmapSource? _imageSource;

    private static string ReviseFilePath(string source)
    {
        var filePath = source;
        if (filePath.Length > 2 && filePath[0] == '"' && filePath[^1] == '"')
            filePath = filePath[1..^1];
        return filePath;
    }

    [RelayCommand]
    private async Task ReadImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ReviseFilePath(ImageFilePath);
        var ppm = await PpmImage.ReadAsync(filePath, cancellationToken);
        if (ppm is not null)
        {
            PpmProp = new PpmProperty(ppm);
            ImageSource = ppm.ToBitmapSource();
        }
        else
        {
            PpmProp = null;
            ImageSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveImage))]
    private async Task SaveImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ReviseFilePath(ImageFilePath);
        if (await PpmImage.ReadAsync(filePath, cancellationToken) is not { } ppm)
            return;

        var filename = $"_output_{DateTime.Now:yyMMdd_HHmmss}.bmp";
        await ppm.SaveToBmpAsync(filename, cancellationToken);
        Debug.WriteLine($"Saved : {filename}");
    }
    bool CanSaveImage() => ImageSource is not null;
}

public sealed record PpmProperty(int FormatNumber, int Width, int Height, int MaxLevel, string? Comment)
{
    public PpmProperty(IPpmImage ppm) : this(ppm.FormatNumber, ppm.Width, ppm.Height, ppm.MaxLevel, ppm.Comment) { }
}
