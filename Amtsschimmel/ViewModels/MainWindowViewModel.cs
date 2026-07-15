using System.Collections.ObjectModel;
using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
    private readonly AmtsblattService _amtsblatt = new();
    private readonly DispatcherTimer _gameTimer;
    private readonly DispatcherTimer _autosaveTimer;
    private readonly DispatcherTimer _tickerTimer;
    private readonly DispatcherTimer _goldenTimer;
    private readonly Random _rng = new();
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private DateTime _lastSaveUtc = DateTime.UtcNow;
    private readonly Queue<double> _incomeSamples = new();
    private int _sampleTickCounter;

    public ObservableCollection<GeneratorViewModel> Generators { get; } = [];
    public ObservableCollection<AchievementViewModel> Achievements { get; } = [];
    public ObservableCollection<ResearchViewModel> Research { get; } = [];

    [ObservableProperty]
    private string _researchCountText = "";

    [ObservableProperty]
    private string _stempelText = "0";

    [ObservableProperty]
    private string _perSecondText = "0/s";

    [ObservableProperty]
    private string _clickPowerText = "1";

    [ObservableProperty]
    private string _autoClickText = "";

    [ObservableProperty]
    private bool _isAutoClickVisible;

    [ObservableProperty]
    private string _nextThresholdText = "";

    /// <summary>Fortschritt zur nächsten Reform-Schwelle (0..1) für den Fortschrittsbalken.</summary>
    [ObservableProperty]
    private double _prestigeProgress;

    // ---- Buff (Goldene Formulare) ----
    [ObservableProperty]
    private string _buffText = "";

    [ObservableProperty]
    private bool _isBuffVisible;

    [ObservableProperty]
    private bool _isGoldenFormVisible;

    /// <summary>Auto-Klicks/s als Zahl (für die Partikel-Animation im Code-Behind).</summary>
    [ObservableProperty]
    private double _autoClicksPerSecondValue;

    /// <summary>Betrag pro Auto-Partikel (bei hoher Rate aggregiert, damit die Summe stimmt).</summary>
    [ObservableProperty]
    private string _autoParticleText = "";

    // ---- Statuszeile ----
    [ObservableProperty]
    private string _tickerText = "Amtsblatt wird zugestellt …";

    [ObservableProperty]
    private string _saveIndicatorText = "";

    // ---- Statistik-Tab ----
    [ObservableProperty]
    private string _statPlayTime = "";

    [ObservableProperty]
    private string _statAllTimeEarned = "";

    [ObservableProperty]
    private string _statRunEarned = "";

    [ObservableProperty]
    private string _statClicks = "";

    [ObservableProperty]
    private string _statHighestIncome = "";

    [ObservableProperty]
    private string _statReformen = "";

    [ObservableProperty]
    private string _statGolden = "";

    [ObservableProperty]
    private string _statPersonal = "";

    [ObservableProperty]
    private string _statResearch = "";

    [ObservableProperty]
    private Avalonia.Points? _sparklinePoints;

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
        foreach (var def in ResearchDefinitions.All)
        {
            Research.Add(new ResearchViewModel(_engine, def));
        }

        ShowOfflineProgress();

        _engine.MilestoneReached += OnMilestoneReached;

        _gameTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Normal, OnGameTick);
        _autosaveTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, (_, _) => Save());
        _tickerTimer = new DispatcherTimer(TimeSpan.FromSeconds(25), DispatcherPriority.Background,
            (_, _) => TickerText = _amtsblatt.NextHeadline(_engine.State));
        _goldenTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, OnGoldenTimer);
        _gameTimer.Start();
        _autosaveTimer.Start();
        _tickerTimer.Start();
        _goldenTimer.Start();
        TickerText = _amtsblatt.NextHeadline(_engine.State);
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
                $"({_engine.OfflineEfficiency:P0} Effizienz, max. {_engine.OfflineCap.TotalHours:0} h).";
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
        PerSecondText = NumberFormatter.Format(_engine.EffectiveIncomePerSecond()) + "/s";
        var autoClicks = _engine.AutoClicksPerSecond;
        IsAutoClickVisible = autoClicks > 0;
        AutoClickText = $"🤖 Stempelautomat: {autoClicks:0.#}×/s (+{NumberFormatter.Format(_engine.ClickPower * autoClicks)}/s)";
        AutoClicksPerSecondValue = autoClicks;
        if (autoClicks > 0)
        {
            // Visuell max. 4 Partikel/s; jedes Partikel trägt den anteiligen Gesamtbetrag.
            var visualRate = Math.Min(autoClicks, 4);
            AutoParticleText = "+" + NumberFormatter.Format(_engine.ClickPower * autoClicks / visualRate);
        }
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
            : $"Noch {NumberFormatter.Format(Math.Max(0, _engine.CurrentPrestigeThreshold - state.TotalEarnedThisRun))} Stempel bis zur nächsten Reform";
        NextThresholdText = $"Aktuelle Reform-Schwelle: {NumberFormatter.Format(_engine.CurrentPrestigeThreshold)} Stempel (verzehnfacht sich mit jeder Reform)";
        PrestigeProgress = Math.Clamp(state.TotalEarnedThisRun / _engine.CurrentPrestigeThreshold, 0, 1);
        AchievementCountText = $"{state.UnlockedAchievements.Count} / {GameDefinitions.Achievements.Count}";
        ResearchCountText = $"{state.ResearchLevels.Values.Sum()} Stufen in {state.ResearchLevels.Count} / {ResearchDefinitions.All.Count} Fortbildungen";

        IsBuffVisible = _engine.IsBuffActive;
        if (IsBuffVisible)
        {
            BuffText = $"⚡ Erlassflut ×{_engine.ActiveBuffFactor:0.#} — noch {_engine.BuffRemaining.TotalSeconds:0} s";
        }
        var sinceSave = (DateTime.UtcNow - _lastSaveUtc).TotalSeconds;
        SaveIndicatorText = $"💾 gespeichert vor {sinceSave:0} s";

        StatPlayTime = FormatDuration(TimeSpan.FromSeconds(state.TotalPlaySeconds));
        StatAllTimeEarned = NumberFormatter.Format(state.TotalEarnedAllTime);
        StatRunEarned = NumberFormatter.Format(state.TotalEarnedThisRun);
        StatClicks = state.TotalClicks.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        StatHighestIncome = NumberFormatter.Format(state.HighestIncomePerSec) + "/s";
        StatReformen = $"{state.TotalReformen} (⌀ {state.Paragraphen} §)";
        StatGolden = state.GoldenFormsClicked.ToString();
        StatPersonal = state.Generators.Values.Sum(g => g.Owned).ToString();
        StatResearch = $"{state.ResearchLevels.Values.Sum()} Stufen";

        // Sparkline: alle ~5 s ein Einkommens-Sample, 60 Samples = 5 Minuten.
        if (++_sampleTickCounter >= 50)
        {
            _sampleTickCounter = 0;
            _incomeSamples.Enqueue(_engine.EffectiveIncomePerSecond());
            while (_incomeSamples.Count > 60)
            {
                _incomeSamples.Dequeue();
            }
            SparklinePoints = BuildSparkline();
        }

        foreach (var generator in Generators)
        {
            generator.Refresh();
        }
        foreach (var research in Research)
        {
            research.Refresh();
        }
        foreach (var achievement in Achievements)
        {
            if (!achievement.IsUnlocked && state.UnlockedAchievements.Contains(achievement.Definition.Id))
            {
                achievement.IsUnlocked = true;
            }
        }
    }

    private void OnAchievementUnlocked(AchievementDefinition achievement) =>
        ShowToast($"🏆 {achievement.Name} — {achievement.Description} (+1 % Produktion)");

    private void OnMilestoneReached(GeneratorDefinition def, long threshold) =>
        ShowToast($"🏅 Beförderung! {threshold}× {def.Name} — Produktion des Typs verdoppelt!");

    private void ShowToast(string text)
    {
        ToastText = text;
        IsToastVisible = true;
        DispatcherTimer.RunOnce(() => IsToastVisible = false, TimeSpan.FromSeconds(5));
    }

    /// <summary>Alle 30 s Spawn-Chance (~alle 3 Min. eins); verschwindet nach 12 s von selbst.</summary>
    private void OnGoldenTimer(object? sender, EventArgs e)
    {
        if (IsGoldenFormVisible || _rng.Next(100) >= 17)
        {
            return;
        }
        IsGoldenFormVisible = true;
        DispatcherTimer.RunOnce(() => IsGoldenFormVisible = false, TimeSpan.FromSeconds(12));
    }

    [RelayCommand]
    private void ClickGoldenForm()
    {
        if (!IsGoldenFormVisible)
        {
            return;
        }
        IsGoldenFormVisible = false;
        ShowToast(_engine.GrantGoldenFormReward(_rng));
    }

    [RelayCommand]
    private void Stamp() => _engine.Click();

    // ---------- Fenster-Commands (eigene Titelleiste, Projektstandard) ----------

    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    [RelayCommand]
    private void ShowInfo()
    {
        var window = new Views.InfoWindow { DataContext = new InfoViewModel() };
        if (MainWindow is { } owner)
        {
            window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }

    [RelayCommand]
    private void Minimize()
    {
        if (MainWindow is { } window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    [RelayCommand]
    private void Maximize()
    {
        if (MainWindow is { } window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    [RelayCommand]
    private void Close() => MainWindow?.Close();

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

    private Avalonia.Points? BuildSparkline()
    {
        if (_incomeSamples.Count < 2)
        {
            return null;
        }
        const double width = 600, height = 70;
        var samples = _incomeSamples.ToArray();
        var max = Math.Max(samples.Max(), 1e-9);
        var points = new Avalonia.Points();
        for (var i = 0; i < samples.Length; i++)
        {
            var x = i * width / (samples.Length - 1);
            var y = height - samples[i] / max * (height - 4) - 2;
            points.Add(new Avalonia.Point(x, y));
        }
        return points;
    }

    private static string FormatDuration(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes} min" : $"{t.Minutes} min {t.Seconds} s";

    public void Save()
    {
        _saveGame.Save(_engine.State);
        _lastSaveUtc = DateTime.UtcNow;
    }

    /// <summary>Beim Fensterschließen aufräumen und final speichern.</summary>
    public void Shutdown()
    {
        _gameTimer.Stop();
        _autosaveTimer.Stop();
        Save();
        Log.Info("Spiel beendet, Spielstand gespeichert.");
    }
}
