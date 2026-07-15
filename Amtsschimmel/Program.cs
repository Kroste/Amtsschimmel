using Avalonia;
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
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

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
