using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using AIFractureDetection.App.Models;
using AIFractureDetection.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIFractureDetection.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INiftiReader _niftiReader;
    private readonly IApiService _apiService;
    private readonly IReportService _reportService;
    private readonly IThemeService _themeService;

    public MainViewModel(
        INiftiReader niftiReader,
        IApiService apiService,
        IReportService reportService,
        IThemeService themeService)
    {
        _niftiReader = niftiReader;
        _apiService = apiService;
        _reportService = reportService;
        _themeService = themeService;
        _themeService.Apply(_themeService.Current);
    }

    [ObservableProperty] private string? _selectedFilePath;
    [ObservableProperty] private string? _selectedFileName;
    [ObservableProperty] private string _selectedFileSizeText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Başlamak için bir .nii.gz dosyası seçin veya sürükleyin.";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private NiftiImage? _niftiImage;
    [ObservableProperty] private BitmapSource? _previewImage;
    [ObservableProperty] private int _currentSlice;
    [ObservableProperty] private int _maxSlice;
    [ObservableProperty] private SliceOrientation _orientation = SliceOrientation.Axial;
    [ObservableProperty] private DetectionResult? _result;
    [ObservableProperty] private string _apiBaseUrl = "http://127.0.0.1:8000";
    [ObservableProperty] private bool _apiReachable;
    private bool _isOverlayActive;
    public bool IsOverlayActive
    {
        get => _isOverlayActive;
        set => SetProperty(ref _isOverlayActive, value);
    }

    public bool HasFile => !string.IsNullOrEmpty(SelectedFilePath);
    public bool HasResult => Result is not null;

    partial void OnSelectedFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasFile));
        AnalyzeCommand.NotifyCanExecuteChanged();
    }

    partial void OnResultChanged(DetectionResult? value)
    {
        OnPropertyChanged(nameof(HasResult));
        ExportReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentSliceChanged(int value)
    {
        if (!_isOverlayActive)
            UpdatePreview();
    }

    partial void OnOrientationChanged(SliceOrientation value)
    {
        if (NiftiImage is null) return;
        MaxSlice = value switch
        {
            SliceOrientation.Axial => NiftiImage.Depth - 1,
            SliceOrientation.Coronal => NiftiImage.Height - 1,
            SliceOrientation.Sagittal => NiftiImage.Width - 1,
            _ => 0
        };
        CurrentSlice = MaxSlice / 2;
        IsOverlayActive = false;
        UpdatePreview();
    }

    [RelayCommand]
    private void PickFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "NIfTI dosyası seçin",
            Filter = "NIfTI görüntüleri (*.nii;*.nii.gz)|*.nii;*.nii.gz|Tüm dosyalar (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
            _ = LoadFileAsync(dialog.FileName);
    }

    [RelayCommand]
    private void Clear()
    {
        SelectedFilePath = null;
        SelectedFileName = null;
        SelectedFileSizeText = string.Empty;
        NiftiImage = null;
        PreviewImage = null;
        Result = null;
        Progress = 0;
        IsOverlayActive = false;
        StatusMessage = "Başlamak için bir .nii.gz dosyası seçin veya sürükleyin.";
    }

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath)) return;
        try
        {
            IsBusy = true;
            Progress = 0;
            Result = null;
            IsOverlayActive = false;
            StatusMessage = "AI servisine gönderiliyor...";

            _apiService.SetBaseUrl(ApiBaseUrl);
            var progress = new Progress<double>(p => Progress = p * 100);
            Result = await _apiService.AnalyzeAsync(SelectedFilePath, progress);

            StatusMessage = Result.HasPositiveFindings
                ? $"{Result.Findings.Count} bulgu tespit edildi."
                : "Belirgin bir anomali tespit edilmedi.";
        }
        catch (HttpRequestException ex)
        {
            Result = new DetectionResult { Status = "error", Message = ex.Message };
            StatusMessage = $"API hatası: {ex.Message}";
        }
        catch (Exception ex)
        {
            Result = new DetectionResult { Status = "error", Message = ex.Message };
            StatusMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAnalyze() => true;

    [RelayCommand]
    private void GoToFinding(Finding finding)
    {
        if (finding is null) return;

        if (finding.OverlayImage is not null)
        {
            ShowOverlay(finding.OverlayImage);
            IsOverlayActive = true;
        }

        if (finding.SliceIndex is int idx)
        {
            CurrentSlice = Math.Clamp(idx, 0, MaxSlice);
        }

        StatusMessage = $"{finding.Label} — Slice {finding.SliceIndex}, {finding.Region}, Güven: {finding.Confidence:P0}";
    }

    [RelayCommand]
    private void ClearOverlay()
    {
        IsOverlayActive = false;
        UpdatePreview();
    }

    private void ShowOverlay(string base64Png)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64Png);
            using var ms = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(
                ms,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
            var bmp = decoder.Frames[0];
            bmp.Freeze();
            PreviewImage = bmp;
        }
        catch { }
    }

    [RelayCommand]
    private async Task PingApiAsync()
    {
        _apiService.SetBaseUrl(ApiBaseUrl);
        StatusMessage = "API kontrol ediliyor...";
        ApiReachable = await _apiService.PingAsync();
        StatusMessage = ApiReachable
            ? $"API erişilebilir: {ApiBaseUrl}"
            : $"API erişilemiyor: {ApiBaseUrl}";
    }

    [RelayCommand]
    private void ToggleTheme() => _themeService.Toggle();

    [RelayCommand(CanExecute = nameof(HasResult))]
    private async Task ExportReportAsync()
    {
        if (Result is null) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Raporu kaydet",
            Filter = "PDF dosyaları (*.pdf)|*.pdf",
            FileName = $"rapor_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            IsBusy = true;
            StatusMessage = "PDF rapor oluşturuluyor...";
            byte[]? previewPng = PreviewImage is not null ? EncodePng(PreviewImage) : null;
            await _reportService.GenerateAsync(
                dialog.FileName,
                SelectedFileName ?? "-",
                Result,
                previewPng);
            StatusMessage = $"Rapor kaydedildi: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rapor hatası: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            if (!IsValidNifti(path))
            {
                StatusMessage = "Geçersiz dosya: yalnızca .nii veya .nii.gz desteklenir.";
                return;
            }
            IsBusy = true;
            Result = null;
            IsOverlayActive = false;
            SelectedFilePath = path;
            SelectedFileName = Path.GetFileName(path);
            var info = new FileInfo(path);
            SelectedFileSizeText = FormatSize(info.Length);
            StatusMessage = "NIfTI dosyası okunuyor...";

            NiftiImage = await _niftiReader.ReadAsync(path);

            MaxSlice = Orientation switch
            {
                SliceOrientation.Axial => NiftiImage.Depth - 1,
                SliceOrientation.Coronal => NiftiImage.Height - 1,
                SliceOrientation.Sagittal => NiftiImage.Width - 1,
                _ => 0
            };
            CurrentSlice = MaxSlice / 2;
            UpdatePreview();
            StatusMessage = $"Hazır: {NiftiImage.Width}×{NiftiImage.Height}×{NiftiImage.Depth} voksel.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Okuma hatası: {ex.Message}";
            SelectedFilePath = null;
            NiftiImage = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public static bool IsValidNifti(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.EndsWith(".nii") || lower.EndsWith(".nii.gz");
    }

    private void UpdatePreview()
    {
        if (NiftiImage is null) { PreviewImage = null; return; }
        try
        {
            var bytes = NiftiImage.GetSlice(Orientation, CurrentSlice, out int w, out int h);
            if (bytes.Length == 0) { PreviewImage = null; return; }
            var bmp = BitmapSource.Create(w, h, 96, 96,
                System.Windows.Media.PixelFormats.Gray8, null, bytes, w);
            bmp.Freeze();
            PreviewImage = bmp;
        }
        catch { PreviewImage = null; }
    }

    private static byte[] EncodePng(BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
        return $"{size:0.##} {units[u]}";
    }
}
