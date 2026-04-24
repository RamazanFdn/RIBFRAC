using AIFractureDetection.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AIFractureDetection.App.Services;

public class ReportService : IReportService
{
    public Task GenerateAsync(
        string outputPath,
        string sourceFileName,
        DetectionResult result,
        byte[]? previewPng,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(h =>
                    {
                        h.Column(col =>
                        {
                            col.Item().Text("AI Kırık & Çıkık Tespit Raporu")
                                .FontSize(18).SemiBold().FontColor(Colors.Indigo.Darken2);
                            col.Item().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // Dosya bilgisi
                        col.Item().Element(e => SectionHeader(e, "Dosya Bilgisi"));
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });
                            TableRow(t, "Kaynak Dosya", sourceFileName);
                            TableRow(t, "Model Sürümü", result.ModelVersion ?? "-");
                            TableRow(t, "İşlem Süresi", $"{result.InferenceTimeMs:F0} ms");
                            TableRow(t, "Genel Güven", $"{result.OverallConfidence * 100:F1}%");
                        });

                        // Özet
                        col.Item().PaddingTop(15).Element(e => SectionHeader(e, "Özet"));
                        var summaryColor = result.HasPositiveFindings ? Colors.Red.Darken2 : Colors.Green.Darken2;
                        col.Item().Text(text =>
                        {
                            text.Span(result.HasPositiveFindings
                                ? $"⚠ {result.Findings.Count} anomali tespit edildi."
                                : "✓ Belirgin bir anomali tespit edilmedi.")
                                .FontColor(summaryColor).SemiBold();
                        });

                        // Genel önizleme
                        if (previewPng is { Length: > 0 })
                        {
                            col.Item().PaddingTop(15).Element(e => SectionHeader(e, "Görüntü Önizleme"));
                            col.Item().AlignCenter().Width(350).Image(previewPng);
                        }

                        // Bulgular tablosu
                        if (result.HasPositiveFindings)
                        {
                            col.Item().PaddingTop(15).Element(e => SectionHeader(e, "Bulgular"));
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Element(HeaderCell).Text("Tip");
                                    h.Cell().Element(HeaderCell).Text("Etiket");
                                    h.Cell().Element(HeaderCell).Text("Bölge");
                                    h.Cell().Element(HeaderCell).Text("Slice");
                                    h.Cell().Element(HeaderCell).Text("Güven");
                                });
                                foreach (var f in result.Findings)
                                {
                                    t.Cell().Element(BodyCell).Text(TranslateType(f.Type));
                                    t.Cell().Element(BodyCell).Text(f.Label);
                                    t.Cell().Element(BodyCell).Text(f.Region ?? "-");
                                    t.Cell().Element(BodyCell).Text(f.SliceIndex?.ToString() ?? "-");
                                    t.Cell().Element(BodyCell).Text($"{f.Confidence * 100:F1}%");
                                }
                            });

                            // Her bulgu için overlay görüntüsü
                            col.Item().PaddingTop(20).Element(e => SectionHeader(e, "Kırık Lokalizasyonu"));
                            col.Item().PaddingTop(10).Text(
                                "Aşağıdaki görüntülerde tespit edilen kırık bölgeleri kırmızı ile işaretlenmiştir.")
                                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

                            var findingsWithOverlay = result.Findings
                                .Where(f => !string.IsNullOrEmpty(f.OverlayImage))
                                .ToList();

                            if (findingsWithOverlay.Any())
                            {
                                col.Item().PaddingTop(10).Grid(grid =>
                                {
                                    grid.Columns(2);
                                    grid.Spacing(10);

                                    foreach (var f in findingsWithOverlay)
                                    {
                                        var imageBytes = Convert.FromBase64String(f.OverlayImage!);
                                        grid.Item().Column(innerCol =>
                                        {
                                            innerCol.Item().Text($"{f.Label}")
                                                .FontSize(10).SemiBold().FontColor(Colors.Indigo.Darken2);
                                            innerCol.Item().Text($"Bölge: {f.Region} • Slice: {f.SliceIndex} • Güven: {f.Confidence * 100:F0}%")
                                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                                            innerCol.Item().PaddingTop(4).Image(imageBytes);
                                        });
                                    }
                                });
                            }
                        }

                        // Uyarı
                        col.Item().PaddingTop(25).Text(
                            "Bu rapor yapay zekâ destekli bir ön değerlendirmedir. " +
                            "Klinik karar vermek için nitelikli bir sağlık profesyoneline başvurulmalıdır.")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ").FontSize(9);
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" / ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            }).GeneratePdf(outputPath);
        }, cancellationToken);
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).Padding(5).AlignCenter();

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

    private static void SectionHeader(IContainer container, string title)
    {
        container.BorderBottom(1).BorderColor(Colors.Indigo.Medium).PaddingBottom(3)
            .Text(title).FontSize(13).SemiBold().FontColor(Colors.Indigo.Darken2);
    }

    private static void TableRow(TableDescriptor table, string key, string value)
    {
        table.Cell().Padding(3).Text(key).SemiBold();
        table.Cell().Padding(3).Text(value);
    }

    private static string TranslateType(string type) => type.ToLowerInvariant() switch
    {
        "fracture" => "Kırık",
        "dislocation" => "Çıkık",
        _ => type
    };
}
