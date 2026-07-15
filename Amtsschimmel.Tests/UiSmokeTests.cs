using Amtsschimmel.Services;
using Amtsschimmel.ViewModels;
using Amtsschimmel.Views;
using Avalonia;
using Avalonia.Headless;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// UI-Smoke-Tests: instanziieren die Fenster headless und fangen damit
/// XAML-Populate-Fehler (Binding-Tippfehler, ungültige Property-Werte) in CI ab.
/// </summary>
public sealed class UiSmokeTests
{
    // Avalonia darf pro Prozess nur einmal initialisiert werden → geteilte Session.
    private static readonly HeadlessUnitTestSession Session =
        HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));

    [Fact]
    public Task MainWindow_XamlLaedtUndFensterOeffnet() =>
        Session.Dispatch(() =>
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(new GameEngine(), new SaveGameService(),
                new SettingsService(Path.Combine(Path.GetTempPath(), "amt-test-" + Guid.NewGuid())), new UpdateCheckService()),
            };
            window.Show();
            window.Close();
        }, CancellationToken.None);

    [Fact]
    public Task SettingsWindow_XamlLaedtUndFensterOeffnet() =>
        Session.Dispatch(() =>
        {
            var window = new SettingsWindow
            {
                DataContext = new SettingsViewModel(new GameEngine(), new SaveGameService(),
                    new SettingsService(Path.Combine(Path.GetTempPath(), "amt-test-" + Guid.NewGuid()))),
            };
            window.Show();
            window.Close();
        }, CancellationToken.None);

    [Fact]
    public Task InfoWindow_XamlLaedtUndFensterOeffnet() =>
        Session.Dispatch(() =>
        {
            var window = new InfoWindow { DataContext = new InfoViewModel() };
            window.Show();
            window.Close();
        }, CancellationToken.None);
}
