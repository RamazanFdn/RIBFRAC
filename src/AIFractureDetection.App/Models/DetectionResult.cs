using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AIFractureDetection.App.Models;

/// <summary>
/// AI servisinden dönen analiz sonucu.
/// Python tarafındaki FastAPI sözleşmesi ile uyumlu.
/// </summary>
public class DetectionResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("model_version")]
    public string? ModelVersion { get; set; }

    [JsonPropertyName("inference_time_ms")]
    public double InferenceTimeMs { get; set; }

    [JsonPropertyName("findings")]
    public List<Finding> Findings { get; set; } = new();

    [JsonPropertyName("overall_confidence")]
    public double OverallConfidence { get; set; }

    /// <summary>
    /// En az bir kırık/çıkık tespit edildi mi?
    /// </summary>
    [JsonIgnore]
    public bool HasPositiveFindings => Findings.Count > 0;
}

/// <summary>
/// Tespit edilen bir anomali (kırık veya çıkık).
/// </summary>
public class Finding
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "fracture" veya "dislocation"

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("bbox")]
    public BoundingBox? BoundingBox { get; set; }

    [JsonPropertyName("slice_index")]
    public int? SliceIndex { get; set; }
}

/// <summary>
/// Bulgunun görüntü üzerindeki konumu (voxel cinsinden).
/// </summary>
public class BoundingBox
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("z")]
    public int Z { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("depth")]
    public int Depth { get; set; }
}
