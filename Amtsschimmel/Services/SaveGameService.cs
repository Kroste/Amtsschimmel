using System.Text;
using System.Text.Json;
using Amtsschimmel.Models;
using NLog;

namespace Amtsschimmel.Services;

/// <summary>Persistiert den Spielstand als JSON im AppData-Verzeichnis.</summary>
public sealed class SaveGameService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _savePath;

    public SaveGameService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Amtsschimmel");
        Directory.CreateDirectory(dir);
        _savePath = Path.Combine(dir, "savegame.json");
    }

    public GameState Load()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                var json = File.ReadAllText(_savePath);
                var state = JsonSerializer.Deserialize<GameState>(json, JsonOptions);
                if (state is not null)
                {
                    Log.Info("Spielstand geladen: {Path}", _savePath);
                    return state;
                }
            }
        }
        catch (Exception ex)
        {
            // Korrupter Spielstand darf den Start nicht verhindern — Backup anlegen.
            Log.Error(ex, "Spielstand konnte nicht geladen werden, starte neu.");
            TryBackupCorruptSave();
        }
        return new GameState();
    }

    public void Save(GameState state)
    {
        try
        {
            state.LastSavedUtc = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(state, JsonOptions);
            // Atomar schreiben: erst temporär, dann ersetzen — verhindert halbe Saves bei Absturz.
            var tmp = _savePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _savePath, overwrite: true);
            Log.Debug("Spielstand gespeichert.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Speichern fehlgeschlagen.");
        }
    }

    // ---------- Spielstand-Transfer (Base64, z. B. Arbeits-Laptop ↔ Bazzite) ----------

    private const string ExportPrefix = "AMT1:";

    /// <summary>Serialisiert den Spielstand als kopierbaren Base64-String mit Format-Präfix.</summary>
    public string Export(GameState state) =>
        ExportPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state, JsonOptions)));

    /// <summary>Parst einen Export-String; null bei falschem Präfix oder kaputten Daten.</summary>
    public GameState? TryImport(string input)
    {
        try
        {
            var trimmed = input.Trim();
            if (!trimmed.StartsWith(ExportPrefix, StringComparison.Ordinal))
            {
                return null;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(trimmed[ExportPrefix.Length..]));
            return JsonSerializer.Deserialize<GameState>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Spielstand-Import fehlgeschlagen.");
            return null;
        }
    }

    private void TryBackupCorruptSave()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                File.Move(_savePath, _savePath + $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
            }
        }
        catch
        {
            // Backup ist Best-Effort.
        }
    }
}
