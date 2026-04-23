namespace AIFractureDetection.App.Services;

public enum AppTheme
{
    Dark,
    Light
}

public interface IThemeService
{
    AppTheme Current { get; }
    event EventHandler<AppTheme>? ThemeChanged;
    void Apply(AppTheme theme);
    void Toggle();
}
