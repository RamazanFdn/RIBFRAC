using System.Windows;
using AIFractureDetection.App.Services;
using AIFractureDetection.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AIFractureDetection.App;

/// <summary>
/// Uygulama giriş noktası. Dependency Injection ve global servis kayıtlarını yönetir.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services => ((App)Current)._host!.Services;

    protected override void OnStartup(StartupEventArgs e)
    {
        // QuestPDF lisans ayarı (Community)
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // HTTP client (Python FastAPI backend).
                // BaseAddress burada ayarlanmaz; ApiService çalışma anında tam URL kurar.
                services.AddHttpClient<IApiService, ApiService>(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                });

                // Servisler
                services.AddSingleton<INiftiReader, NiftiReader>();
                services.AddSingleton<IReportService, ReportService>();
                services.AddSingleton<IThemeService, ThemeService>();

                // ViewModel'ler
                services.AddTransient<MainViewModel>();
            })
            .Build();

        // Ana pencereyi DI üzerinden oluştur
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
