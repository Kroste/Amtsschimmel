using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;

namespace Amtsschimmel.ViewModels;

/// <summary>ViewModel der InfoBox (Über-Fenster) nach Projektstandard.</summary>
public sealed partial class InfoViewModel : ObservableObject
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string GithubUrl = "https://github.com/Kroste/Amtsschimmel";
    private const string CoffeeUrl = "https://buymeacoffee.com/kroste";

    public string Name => "Amtsschimmel";

    public string VersionText { get; } = "Version " + ResolveVersion();

    public string Description =>
        "Das Behörden-Incremental: Stemple Formulare, stelle Personal ein, " +
        "erforsche die Verwaltungsakademie — und reformiere dich zur Exzellenz.";

    private static string ResolveVersion()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(info))
        {
            return "1.0";
        }
        // Build-Metadaten (+sha) abschneiden.
        var plus = info.IndexOf('+');
        return plus > 0 ? info[..plus] : info;
    }

    [RelayCommand]
    private void OpenGitHub() => OpenUrl(GithubUrl);

    [RelayCommand]
    private void OpenBuyMeACoffee() => OpenUrl(CoffeeUrl);

    private static void OpenUrl(string url)
    {
        // Bevorzugt der Avalonia-Launcher (nutzt unter Linux das xdg-Portal),
        // Fallback auf den System-Handler.
        try
        {
            var mainWindow = (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow is not null && TopLevel.GetTopLevel(mainWindow)?.Launcher is { } launcher)
            {
                _ = launcher.LaunchUriAsync(new Uri(url));
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Avalonia-Launcher fehlgeschlagen, nutze Prozess-Fallback.");
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "URL konnte nicht geöffnet werden: {Url}", url);
        }
    }
}
