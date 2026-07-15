using System.Text.Json;
using NLog;

namespace Amtsschimmel.Services;

/// <summary>Anwendungseinstellungen (nicht der Spielstand).</summary>
public sealed class AppSettings
{
    public int AutosaveIntervalSeconds { get; set; } = 30;
    public bool TickerEnabled { get; set; } = true;
    public bool CheckUpdatesOnStartup { get; set; } = true;
}

/// <summary>Lädt/speichert Einstellungen als JSON und meldet Änderungen.</summary>
public sealed class SettingsService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public AppSettings Current { get; private set; } = new();

    /// <summary>Wird nach jedem erfolgreichen Speichern gefeuert (für Live-Übernahme).</summary>
    public event Action? Changed;

    /// <param name="directory">Abweichendes Verzeichnis (nur für Tests); Standard: AppData/Amtsschimmel.</param>
    public SettingsService(string? directory = null)
    {
        var dir = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Amtsschimmel");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Einstellungen konnten nicht geladen werden, nutze Standardwerte.");
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, JsonOptions));
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Einstellungen konnten nicht gespeichert werden.");
        }
    }
}
