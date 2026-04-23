using AIFractureDetection.App.Models;

namespace AIFractureDetection.App.Services;

public interface IApiService
{
    /// <summary>
    /// Python AI servisinin durumunu kontrol eder.
    /// </summary>
    Task<bool> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// NIfTI dosyasını backend'e yükler ve kırık/çıkık analizi sonucunu döner.
    /// </summary>
    Task<DetectionResult> AnalyzeAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API'nın base URL'ini günceller.
    /// </summary>
    void SetBaseUrl(string url);
}
