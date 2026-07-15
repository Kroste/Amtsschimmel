using System.Collections.ObjectModel;
using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NLog;

namespace Amtsschimmel.ViewModels;

/// <summary>
/// Hauptfenster-ViewModel: hält den Game-Loop (10 Ticks/s), Autosave (30 s)
/// und aggregiert alle Anzeigewerte.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly GameEngine _engine;
    private readonly SaveGameService _saveGame;
    private readonly DispatcherTimer _gameTimer;
    private readonly DispatcherTimer _autosaveTimer;
    private DateTime _lastTickUtc = DateTime.UtcNow;

    public ObservableCollection<GeneratorViewModel> Generators { get; } = [];
    public ObservableCollection<AchievementViewModel> Achievements { get; } = [];

    [ObservableProperty]
    private string _stempelText = "0";

    [ObservableProperty]
    private string _perSecondText = "0/s";

    [ObservableProperty]
    private string _clickPowerText = "1";

    [ObservableProperty]
    private string _clickUpgradeCostText = "";

    [ObservableProperty]
    private bool _canBuyClickUpgrade;

    [ObservableProperty]
    private int _clickUpgradeLevel;

    [ObservableProperty]
    private int _paragraphen;

    [ObservableProperty]
    private string _globalMultiplierText = "×1";

    [ObservableProperty]
    private int _pendingParagraphen;

    [ObservableProperty]
    private bool _canPrestige;

    [ObservableProperty]
    private string _prestigeProgressText = "";

    [ObservableProperty]
    private int _totalReformen;

    [ObservableProperty]
    private string _achievementCountText = "";

    [ObservableProperty]
    private string _toastText = "";

    [ObservableProperty]
    private bool _isToastVisible;

    [ObservableProperty]
    private string _offlineReportText = "";

    [ObservableProperty]
    private bool _isOfflineReportVisible;

    public MainWindowViewModel(GameEngine engine, SaveGameService saveGame)
    {
        _engine = engine;
        _saveGame = saveGame;

        _engine.LoadState(_saveGame.Load());
        _engine.AchievementUnlocked += OnAchievementUnlocked;

        foreach (var def in GameDefinitions.Generators)
        {
            Generators.Add(new GeneratorViewModel(_engine, def));
        }
        foreach (var def in GameDefinitions.Achievements)
        {
            Achievements.Add(new AchievementViewModel(def));
        }

        ShowOfflineProgress();

        _gameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, OnGameTick);
        _autosaveTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, (_, _) => Save());
        _gameTimer.Start();
        _autosaveTimer.Start();
        RefreshAll();
    }

    private void ShowOfflineProgress()
    {
        var offline = _engine.ApplyOfflineProgress();
        if (offline is var (duration, earned) && earned > 0)
        {
            OfflineReportText =
                $"Willkommen zurück! Während deiner Abwesenheit ({duration.Hours} h {duration.Minutes} min) " +
                $"hat dein Amt {NumberFormatter.Format(earned)} Stempel produziert " +
                $"({GameEngine.OfflineEfficiency:P0} Effizienz, max. {GameEngine.OfflineCap.TotalHours:0} h).";
            IsOfflineReportVisible = true;
        }
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        // Echte Delta-Zeit statt fixem Intervall — robust gegen Timer-Jitter und Lastspitzen.
        var now = DateTime.UtcNow;
        var delta = (now - _lastTickUtc).TotalSeconds;
        _lastTickUtc = now;
        _engine.Tick(Math.Clamp(delta, 0, 1));
        RefreshAll();
    }

    private void RefreshAll()
    {
        var state = _engine.State;
        StempelText = NumberFormatter.Format(state.Stempel);
        PerSecondText = NumberFormatter.Format(_engine.TotalProductionPerSecond()) + "/s";
        ClickPowerText = NumberFormatter.Format(_engine.ClickPower);
        ClickUpgradeCostText = NumberFormatter.Format(_engine.ClickUpgradeCost);
        CanBuyClickUpgrade = _engine.CanAfford(_engine.ClickUpgradeCost);
        ClickUpgradeLevel = state.ClickUpgradeLevel;
        Paragraphen = state.Paragraphen;
        GlobalMultiplierText = "×" + _engine.GlobalMultiplier.ToString("0.00");
        PendingParagraphen = _engine.PendingParagraphen;
        CanPrestige = _engine.CanPrestige;
        TotalReformen = state.TotalReformen;
        PrestigeProgressText = _engine.CanPrestige
            ? $"Reform bereit: +{PendingParagraphen} §"
            : $"Noch {NumberFormatter.Format(Math.Max(0, GameEngine.PrestigeThreshold - state.TotalEarnedThisRun))} Stempel bis zur ersten Reform";
        AchievementCountText = $"{state.UnlockedAchievements.Count} / {GameDefinitions.Achievements.Count}";

        foreach (var generator in Generators)
        {
            generator.Refresh();
        }
        foreach (var achievement in Achievements)
        {
            if (!achievement.IsUnlocked && state.UnlockedAchievements.Contains(achievement.Definition.Id))
            {
                achievement.IsUnlocked = true;
            }
        }
    }

    private void OnAchievementUnlocked(AchievementDefinition achievement)
    {
        ToastText = $"🏆 {achievement.Name} — {achievement.Description} (+1 % Produktion)";
        IsToastVisible = true;
        // Toast nach 5 s ausblenden.
        DispatcherTimer.RunOnce(() => IsToastVisible = false, TimeSpan.FromSeconds(5));
    }

    [RelayCommand]
    private void Stamp() => _engine.Click();

    [RelayCommand]
    private void BuyClickUpgrade() => _engine.BuyClickUpgrade();

    [RelayCommand]
    private void Prestige()
    {
        var earned = _engine.Prestige();
        if (earned > 0)
        {
            ToastText = $"📜 Verwaltungsreform! +{earned} Paragraphen — dauerhafter Bonus: {GlobalMultiplierText}";
            IsToastVisible = true;
            DispatcherTimer.RunOnce(() => IsToastVisible = false, TimeSpan.FromSeconds(5));
            Save();
            RefreshAll();
        }
    }

    [RelayCommand]
    private void DismissOfflineReport() => IsOfflineReportVisible = false;

    public void Save() => _saveGame.Save(_engine.State);

    /// <summary>Beim Fensterschließen aufräumen und final speichern.</summary>
    public void Shutdown()
    {
        _gameTimer.Stop();
        _autosaveTimer.Stop();
        Save();
        Log.Info("Spiel beendet, Spielstand gespeichert.");
    }
}
