using AIFractureDetection.App.Models;

namespace AIFractureDetection.App.Services;

public interface INiftiReader
{
    /// <summary>
    /// Verilen .nii veya .nii.gz dosyasını okur ve NiftiImage nesnesine dönüştürür.
    /// </summary>
    Task<NiftiImage> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
