# AI Fracture Detection

Yapay zekâ destekli **kırık ve çıkık tespit** uygulaması. Kullanıcı `.nii` / `.nii.gz` formatındaki 3D medikal görüntüleri sürükle-bırak ya da dosya seç seçeneğiyle yükler; uygulama görüntüyü Python tarafındaki AI servisine gönderir, sonucu UI üzerinde gösterir ve istenirse PDF rapor üretir.

> Bu repo projenin **masaüstü uygulaması** (WPF / .NET 8) kısmını içerir. AI tarafı ayrı bir Python servisi olarak geliştirilmektedir ve HTTP (FastAPI) üzerinden bu uygulamaya bağlanır.

## Özellikler

- `.nii` ve `.nii.gz` dosyaları için sürükle-bırak ve dosya seçici
- NIfTI-1 başlığını yerel olarak okuyup 2D önizleme (axial / coronal / sagittal)
- Eksen ve kesit kaydırıcı ile etkileşimli gezinme
- Python AI servisine HTTP ile yükleme (upload ilerleme göstergesi)
- Tespit edilen bulguların listesi, güven skorları ve bölgesel bilgileri
- Koyu / Açık tema desteği (runtime geçiş)
- QuestPDF ile profesyonel PDF rapor çıktısı

## Mimari

```
┌─────────────────────────┐        HTTP / multipart         ┌──────────────────────────┐
│  WPF .NET 8 Masaüstü    │ ───────────────────────────────▶ │  Python FastAPI AI API   │
│  (bu repo)              │ ◀─────────────────────────────── │  (arkadaşın reposu)      │
└─────────────────────────┘          JSON sonuç              └──────────────────────────┘
```

### API Sözleşmesi

#### `GET /health`
```json
{ "status": "ok" }
```

#### `POST /analyze`
`multipart/form-data` ile `file` alanında `.nii.gz` dosyası gönderilir.

Yanıt formatı:
```json
{
  "status": "ok",
  "message": null,
  "model_version": "1.0.0",
  "inference_time_ms": 1240,
  "overall_confidence": 0.92,
  "findings": [
    {
      "type": "fracture",
      "label": "Distal radius fracture",
      "confidence": 0.91,
      "region": "Sağ el bileği",
      "bbox": { "x": 120, "y": 95, "z": 48, "width": 34, "height": 22, "depth": 8 },
      "slice_index": 48
    }
  ]
}
```

## Proje Yapısı

```
AIFractureDetection.sln
└── src/
    └── AIFractureDetection.App/
        ├── App.xaml / App.xaml.cs            # DI konfigürasyonu
        ├── MainWindow.xaml / .xaml.cs        # Ana pencere + drag-drop
        ├── Models/                           # DetectionResult, NiftiImage vb.
        ├── ViewModels/                       # MainViewModel (MVVM)
        ├── Services/
        │   ├── NiftiReader                   # .nii.gz parser
        │   ├── ApiService                    # HTTP istemcisi
        │   ├── ReportService                 # QuestPDF
        │   └── ThemeService                  # Koyu/Açık tema
        ├── Helpers/                          # Converter'lar
        └── Resources/                        # XAML tema sözlükleri
```

## Geliştirme

### Gereksinimler

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 veya Rider (opsiyonel, CLI da yeterli)

### Derleme ve Çalıştırma

```powershell
git clone https://github.com/<kullanici>/AI-Fracture-Detection.git
cd "AI Fracture Detection"
dotnet restore
dotnet build
dotnet run --project src/AIFractureDetection.App/AIFractureDetection.App.csproj
```

Uygulama açıldığında üstteki alandan AI API adresini doğrulayın (varsayılan `http://127.0.0.1:8000`) ve **Kontrol Et** butonuna basın.

## Kullanım

1. Üst baradan API adresini girin ve **Kontrol Et** ile bağlantıyı doğrulayın
2. `.nii.gz` dosyasını sürükleyip bırakın veya **Dosya Seç** butonunu kullanın
3. Alt kısımdaki kaydırıcı ve oryantasyon seçici ile görüntüyü inceleyin
4. **Analiz Et** butonuna basın
5. Sonuçlar sağ panelde gösterilir
6. Gerekirse **PDF Rapor Oluştur** ile dışa aktarın

## Yol Haritası

- [ ] Bounding box'ların önizleme üzerinde overlay olarak çizilmesi
- [ ] Çoklu dosya / batch analiz desteği
- [ ] SQLite ile analiz geçmişi
- [ ] DICOM desteği (NIfTI'ye ek olarak)
- [ ] Yerleşik model (ONNX) desteği, API opsiyonel

## Yasal Uyarı

Bu uygulama bir **araştırma ve ön tanı aracıdır**. Klinik karar vermek için mutlaka nitelikli bir sağlık profesyoneline danışılmalıdır.

## Lisans

MIT — bkz. [LICENSE](LICENSE)
