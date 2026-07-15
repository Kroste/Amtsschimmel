using Avalonia;
using Avalonia.Media;
using NLog;

namespace Amtsschimmel;

internal static class Program
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [STAThread]
    public static void Main(string[] args)
    {
        ConfigureLogging();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unbehandelte Exception (AppDomain)");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unbeobachtete Task-Exception");
            e.SetObserved();
        };

        try
        {
            Log.Info("Amtsschimmel startet.");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fataler Fehler beim Start.");
            throw;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        // Farb-Emojis (🏢 📚 📜 🏆 …) brauchen einen expliziten Fallback auf den
        // Color-Emoji-Font des Systems. Ohne diesen kann der Font-Fallback in einem
        // monochromen Font landen — die Piktogramme erscheinen dann einfarbig.
        var emojiFont = OperatingSystem.IsWindows() ? "Segoe UI Emoji"
            : OperatingSystem.IsMacOS() ? "Apple Color Emoji"
            : "Noto Color Emoji";

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                // WithInterFont() setzt die Default-Familie über FontManagerOptions;
                // da wir die Options hier ersetzen, muss Inter erneut angegeben werden.
                DefaultFamilyName = "fonts:Inter#Inter",
                FontFallbacks =
                [
                    new FontFallback { FontFamily = new FontFamily(emojiFont) },
                ],
            })
            .LogToTrace();
    }

    private static void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Amtsschimmel", "logs");
        var fileTarget = new NLog.Targets.FileTarget("file")
        {
            FileName = Path.Combine(logDir, "amtsschimmel-${shortdate}.log"),
            Layout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}${onexception:${newline}${exception:format=tostring}}",
            MaxArchiveFiles = 7,
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
        };
        config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
#if DEBUG
        var consoleTarget = new NLog.Targets.ConsoleTarget("console");
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
#endif
        LogManager.Configuration = config;
    }
}
