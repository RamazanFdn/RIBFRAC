using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AIFractureDetection.App.Models;

namespace AIFractureDetection.App.Services;

/// <summary>
/// Python FastAPI backend'i ile konuşan HTTP istemcisi.
/// Sözleşme:
///   GET  /health   -> { "status": "ok" }
///   POST /analyze  -> multipart (file) -> DetectionResult
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://127.0.0.1:8000";

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // BaseAddress başlangıçta bir kez ayarlanır; sonraki SetBaseUrl çağrılarında
        // BaseAddress değiştirilmez, istekler tam URL olarak kurulur.
    }

    public void SetBaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        _baseUrl = url.TrimEnd('/');
    }

    private string BuildUrl(string path) => $"{_baseUrl}/{path.TrimStart('/')}";

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.GetAsync(BuildUrl("health"), cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DetectionResult> AnalyzeAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(filePath);

        using var content = new MultipartFormDataContent();
        await using var fileStream = File.OpenRead(filePath);
        var streamContent = new ProgressStreamContent(fileStream, 81920, progress);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
        content.Add(streamContent, "file", Path.GetFileName(filePath));

        using var response = await _httpClient.PostAsync(BuildUrl("analyze"), content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DetectionResult>(cancellationToken: cancellationToken);
        return result ?? new DetectionResult { Status = "error", Message = "Boş yanıt." };
    }
}

/// <summary>
/// Upload esnasında ilerleme bildirmek için StreamContent sarmalayıcı.
/// </summary>
internal class ProgressStreamContent : StreamContent
{
    private readonly Stream _stream;
    private readonly int _bufferSize;
    private readonly IProgress<double>? _progress;

    public ProgressStreamContent(Stream stream, int bufferSize, IProgress<double>? progress)
        : base(stream, bufferSize)
    {
        _stream = stream;
        _bufferSize = bufferSize;
        _progress = progress;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
    {
        var buffer = new byte[_bufferSize];
        long totalRead = 0;
        long totalLength = _stream.Length;
        int read;
        while ((read = await _stream.ReadAsync(buffer)) > 0)
        {
            await stream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            if (totalLength > 0)
                _progress?.Report((double)totalRead / totalLength);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _stream.Length;
        return true;
    }
}
