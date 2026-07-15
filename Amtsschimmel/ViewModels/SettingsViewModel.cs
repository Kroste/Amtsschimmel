using Amtsschimmel.Models;
using Amtsschimmel.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Amtsschimmel.ViewModels;

/// <summary>Einstellungen inkl. Spielstand-Export/-Import und Reset.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly GameEngine _engine;
    private readonly SaveGameService _saveGame;
    private readonly SettingsService _settings;
    private bool _suppressWrite;
    private bool _resetArmed;

    [ObservableProperty]
    private decimal _autosaveInterval;

    [ObservableProperty]
    private bool _tickerEnabled;

    [ObservableProperty]
    private bool _checkUpdatesOnStartup;

    [ObservableProperty]
    private string _exportText = "";

    [ObservableProperty]
    private string _importText = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _resetButtonText = "Spielstand zurücksetzen";

    public SettingsViewModel(GameEngine engine, SaveGameService saveGame, SettingsService settings)
    {
        _engine = engine;
        _saveGame = saveGame;
        _settings = settings;
        _suppressWrite = true;
        AutosaveInterval = settings.Current.AutosaveIntervalSeconds;
        TickerEnabled = settings.Current.TickerEnabled;
        CheckUpdatesOnStartup = settings.Current.CheckUpdatesOnStartup;
        _suppressWrite = false;
    }

    partial void OnAutosaveIntervalChanged(decimal value) => WriteSettings();
    partial void OnTickerEnabledChanged(bool value) => WriteSettings();
    partial void OnCheckUpdatesOnStartupChanged(bool value) => WriteSettings();

    private void WriteSettings()
    {
        if (_suppressWrite)
        {
            return;
        }
        _settings.Current.AutosaveIntervalSeconds = (int)Math.Clamp(AutosaveInterval, 10, 300);
        _settings.Current.TickerEnabled = TickerEnabled;
        _settings.Current.CheckUpdatesOnStartup = CheckUpdatesOnStartup;
        _settings.Save();
    }

    [RelayCommand]
    private void GenerateExport()
    {
        ExportText = _saveGame.Export(_engine.State);
        StatusText = "Export erstellt — Text kopieren und am Zielrechner importieren.";
    }

    [RelayCommand]
    private void Import()
    {
        var state = _saveGame.TryImport(ImportText);
        if (state is null)
        {
            StatusText = "❌ Import fehlgeschlagen: Der Text ist kein gültiger Amtsschimmel-Export.";
            return;
        }
        _engine.LoadState(state);
        _saveGame.Save(_engine.State);
        StatusText = "✅ Spielstand importiert und gespeichert.";
        ImportText = "";
    }

    [RelayCommand]
    private void Reset()
    {
        if (!_resetArmed)
        {
            // Zwei-Klick-Bestätigung statt eigenem Dialog.
            _resetArmed = true;
            ResetButtonText = "Wirklich? Erneut klicken löscht ALLES";
            return;
        }
        _engine.LoadState(new GameState());
        _saveGame.Save(_engine.State);
        _resetArmed = false;
        ResetButtonText = "Spielstand zurücksetzen";
        StatusText = "🗑️ Spielstand zurückgesetzt. Das Amt beginnt bei null.";
    }
}
