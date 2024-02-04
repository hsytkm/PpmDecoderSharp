using System.Diagnostics;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PpmDecoderSharp.Wpf;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _imageFilePath = @"assets\ppm\P5-255-coins.pgm";

    [ObservableProperty]
    private PpmProperty? _ppmProp;

    public MainWindowViewModel()
    {
        PropertyChanged += async static (sender, e) =>
        {
            if (e.PropertyName is nameof(BitShift))
                await ((MainWindowViewModel)sender!).ReadBitShiftedImageAsync(default);
        };
    }

    private static string ReviseFilePath(string source)
    {
        var filePath = source;
        if (filePath.Length > 2 && filePath[0] == '"' && filePath[^1] == '"')
            filePath = filePath[1..^1];
        return filePath;
    }

    #region Normalization
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNormalizedImageCommand))]
    private BitmapSource? _normalizedImage;

    [RelayCommand]
    private async Task ReadNormalizedImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ReviseFilePath(ImageFilePath);
        var image = await PpmImageReader.ReadAsync(filePath, cancellationToken);
        if (image is not null)
        {
            PpmProp = new PpmProperty(image);
            NormalizedImage = image.ToBitmapSourceWithNormalization();
        }
        else
        {
            PpmProp = null;
            NormalizedImage = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveNormalizedImage))]
    private async Task SaveNormalizedImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ReviseFilePath(ImageFilePath);
        if (await PpmImageReader.ReadAsync(filePath, cancellationToken) is not { } image)
            return;

        var filename = $"_output_normalize_{DateTime.Now:yyMMdd_HHmmss}.bmp";
        await image.SaveToBmpAsync(filename, cancellationToken);
        Debug.WriteLine($"Saved : {filename}");
    }
    private bool CanSaveNormalizedImage() => NormalizedImage is not null;
    #endregion

    #region BitShift
    [ObservableProperty]
    private int _bitShift;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveBitShiftedImageCommand))]
    private BitmapSource? _bitShiftedImage;

    [RelayCommand]
    private void IncrementBitShift() => BitShift = Math.Min(16, BitShift + 1);

    [RelayCommand]
    private void DecrementBitShift() => BitShift = Math.Max(-16, BitShift - 1);

    private async Task ReadBitShiftedImageAsync(CancellationToken cancellationToken)
    {
        var filePath = ReviseFilePath(ImageFilePath);
        var image = await PpmImageReader.ReadAsync(filePath, cancellationToken);
        if (image is not null)
        {
            PpmProp = new PpmProperty(image);
            BitShiftedImage = image.ToBitmapSourceWithBitShift(BitShift);
        }
        else
        {
            PpmProp = null;
            BitShiftedImage = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveBitShiftedImage))]
    private async Task SaveBitShiftedImageAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        var filePath = ReviseFilePath(ImageFilePath);
        if (await PpmImageReader.ReadAsync(filePath, cancellationToken) is not { } image)
            return;

        var filename = $"_output_bitshift_{DateTime.Now:yyMMdd_HHmmss}.bmp";
        await image.SaveToBmpAsync(filename, cancellationToken);
        Debug.WriteLine($"Saved : {filename}");
    }
    private bool CanSaveBitShiftedImage() => BitShiftedImage is not null;
    #endregion
}

public sealed record PpmProperty(int FormatNumber, int Width, int Height, int MaxLevel, string? Comment)
{
    public PpmProperty(IPpmImage ppm) : this(ppm.FormatNumber, ppm.Width, ppm.Height, ppm.MaxLevel, ppm.Comment) { }
}
