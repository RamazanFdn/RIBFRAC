using AIFractureDetection.App.Models;

namespace AIFractureDetection.App.Services;

public interface IReportService
{
    /// <summary>
    /// Verilen sonuç + önizleme görüntüsünden PDF rapor oluşturur ve belirtilen yola kaydeder.
    /// </summary>
    Task GenerateAsync(
        string outputPath,
        string sourceFileName,
        DetectionResult result,
        byte[]? previewPng,
        CancellationToken cancellationToken = default);
}
