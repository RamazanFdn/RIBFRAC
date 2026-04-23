using System.Windows;

namespace AIFractureDetection.App.Services;

public class ThemeService : IThemeService
{
    public AppTheme Current { get; private set; } = AppTheme.Dark;
    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        if (Application.Current is null) return;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        // Eski tema sözlüğünü kaldır
        for (int i = dictionaries.Count - 1; i >= 0; i--)
        {
            var src = dictionaries[i].Source?.OriginalString ?? string.Empty;
            if (src.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase))
            {
                dictionaries.RemoveAt(i);
            }
        }

        var newDict = new ResourceDictionary
        {
            Source = new Uri(
                theme == AppTheme.Dark
                    ? "pack://application:,,,/Resources/DarkTheme.xaml"
                    : "pack://application:,,,/Resources/LightTheme.xaml",
                UriKind.Absolute)
        };
        // Temanın ControlStyles'tan önce gelmesi lazım ki DynamicResource'lar doğru renkleri alsın.
        dictionaries.Insert(0, newDict);

        Current = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    public void Toggle() => Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
