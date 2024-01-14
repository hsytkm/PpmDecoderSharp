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
    private async Task ReadImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ImageFilePath;
        if (filePath.Length > 2 && filePath[0] == '"' && filePath[^1] == '"')
            filePath = filePath[1..^1];

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
}

public sealed record PpmProperty(int FormatNumber, int Width, int Height, int MaxLevel, string? Comment)
{
    public PpmProperty(PpmImage ppm) : this(ppm.FormatNumber, ppm.Width, ppm.Height, ppm.MaxLevel, ppm.Comment) { }
}
