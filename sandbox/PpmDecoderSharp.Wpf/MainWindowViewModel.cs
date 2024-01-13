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
    private BitmapSource? _imageSource;

    [RelayCommand]
    private async Task ReadImageAsync()
    {
        var filePath = ImageFilePath;
        if (filePath[0] == '"' && filePath[^1] == '"')
            filePath = filePath[1..^1];

        var ppm = await PpmImage.ReadAsync(filePath);
        if (ppm is null)
            return;

        PpmProp = new PpmProperty(ppm);
        ImageSource = PpmImageExtensions.ToBitmapSource(ppm);
    }
}

public sealed record PpmProperty(int FormatNumber, int Width, int Height, int MaxLevel, string? Comment)
{
    public PpmProperty(PpmImage ppm) : this(ppm.FormatNumber, ppm.Width, ppm.Height, ppm.MaxLevel, ppm.Comment) { }
}
