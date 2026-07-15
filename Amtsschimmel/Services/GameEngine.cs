using Amtsschimmel.Models;
using NLog;

namespace Amtsschimmel.Services;

/// <summary>
/// UI-unabhängige Spiellogik: Produktion, Käufe, Prestige, Auto-Buyer, Offline-Fortschritt.
/// Wird vom ViewModel per Timer getickt.
/// </summary>
public sealed class GameEngine
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Prestige wird ab dieser Summe verdienter Stempel möglich.</summary>
    public const double PrestigeThreshold = 1e6;

    /// <summary>Offline-Fortschritt: maximal angerechnete Abwesenheit.</summary>
    public static readonly TimeSpan OfflineCap = TimeSpan.FromHours(8);

    /// <summary>Offline-Fortschritt: Effizienzfaktor gegenüber Online-Produktion.</summary>
    public const double OfflineEfficiency = 0.5;

    public GameState State { get; private set; } = new();

    /// <summary>Wird gefeuert, wenn ein Achievement neu freigeschaltet wurde.</summary>
    public event Action<AchievementDefinition>? AchievementUnlocked;

    public void LoadState(GameState state) => State = state;

    // ---------- Produktion ----------

    /// <summary>Globaler Multiplikator aus Paragraphen (+5 % je) und Achievements (+1 % je).</summary>
    public double GlobalMultiplier =>
        (1.0 + State.Paragraphen * 0.05) * (1.0 + State.UnlockedAchievements.Count * 0.01);

    /// <summary>Produktion eines einzelnen Generators pro Sekunde (inkl. Multiplikatoren).</summary>
    public double ProductionPerSecond(GeneratorDefinition def) =>
        def.BaseProduction * State.GetGenerator(def.Id).Owned * GlobalMultiplier;

    /// <summary>Gesamtproduktion pro Sekunde.</summary>
    public double TotalProductionPerSecond() =>
        GameDefinitions.Generators.Sum(ProductionPerSecond);

    /// <summary>Klickkraft: 2^Upgrade-Stufe, skaliert mit globalem Multiplikator.</summary>
    public double ClickPower => Math.Pow(2, State.ClickUpgradeLevel) * GlobalMultiplier;

    /// <summary>Kosten des nächsten Klick-Upgrades ("Stempelkissen").</summary>
    public double ClickUpgradeCost => 100 * Math.Pow(12, State.ClickUpgradeLevel);

    // ---------- Tick ----------

    /// <summary>Ein Simulationsschritt. <paramref name="deltaSeconds"/> = vergangene Zeit.</summary>
    public void Tick(double deltaSeconds)
    {
        var gain = TotalProductionPerSecond() * deltaSeconds;
        if (gain > 0)
        {
            AddStempel(gain);
        }
        RunAutoBuyers();
        CheckAchievements();
    }

    private void AddStempel(double amount)
    {
        State.Stempel += amount;
        State.TotalEarnedThisRun += amount;
        State.TotalEarnedAllTime += amount;
    }

    // ---------- Aktionen ----------

    public void Click()
    {
        State.TotalClicks++;
        AddStempel(ClickPower);
    }

    public bool CanAfford(double cost) => State.Stempel >= cost;

    /// <summary>Kauft <paramref name="amount"/> Einheiten eines Generators, falls bezahlbar.</summary>
    public bool BuyGenerator(GeneratorDefinition def, int amount = 1)
    {
        var gen = State.GetGenerator(def.Id);
        var cost = def.CostForBulk(gen.Owned, amount);
        if (!CanAfford(cost))
        {
            return false;
        }
        State.Stempel -= cost;
        gen.Owned += amount;
        Log.Debug("Gekauft: {Amount}x {Name} für {Cost:0} Stempel", amount, def.Name, cost);
        return true;
    }

    public bool BuyAutoBuyer(GeneratorDefinition def)
    {
        var gen = State.GetGenerator(def.Id);
        if (gen.AutoBuyerOwned || !CanAfford(def.AutoBuyerCost))
        {
            return false;
        }
        State.Stempel -= def.AutoBuyerCost;
        gen.AutoBuyerOwned = true;
        gen.AutoBuyerEnabled = true;
        Log.Info("Auto-Buyer gekauft: {Name}", def.Name);
        return true;
    }

    public bool BuyClickUpgrade()
    {
        var cost = ClickUpgradeCost;
        if (!CanAfford(cost))
        {
            return false;
        }
        State.Stempel -= cost;
        State.ClickUpgradeLevel++;
        Log.Info("Klick-Upgrade auf Stufe {Level}", State.ClickUpgradeLevel);
        return true;
    }

    private void RunAutoBuyers()
    {
        foreach (var def in GameDefinitions.Generators)
        {
            var gen = State.GetGenerator(def.Id);
            if (gen.AutoBuyerOwned && gen.AutoBuyerEnabled && CanAfford(def.CostFor(gen.Owned)))
            {
                BuyGenerator(def);
            }
        }
    }

    // ---------- Prestige ----------

    /// <summary>Paragraphen, die eine Reform jetzt einbringen würde.</summary>
    public int PendingParagraphen =>
        State.TotalEarnedThisRun < PrestigeThreshold
            ? 0
            : (int)Math.Floor(Math.Sqrt(State.TotalEarnedThisRun / PrestigeThreshold));

    public bool CanPrestige => PendingParagraphen > 0;

    /// <summary>
    /// Verwaltungsreform: setzt Stempel, Generatoren und Klick-Upgrade zurück,
    /// gewährt Paragraphen. Achievements und Auto-Buyer-Besitz bleiben erhalten.
    /// </summary>
    public int Prestige()
    {
        var earned = PendingParagraphen;
        if (earned <= 0)
        {
            return 0;
        }

        State.Paragraphen += earned;
        State.TotalReformen++;
        State.Stempel = 0;
        State.TotalEarnedThisRun = 0;
        State.ClickUpgradeLevel = 0;
        foreach (var gen in State.Generators.Values)
        {
            gen.Owned = 0;
            // Auto-Buyer bleiben gekauft — das ist die Belohnung fürs Reformieren.
        }
        Log.Info("Verwaltungsreform #{Count}: +{Earned} Paragraphen (gesamt {Total})",
            State.TotalReformen, earned, State.Paragraphen);
        return earned;
    }

    // ---------- Offline-Fortschritt ----------

    /// <summary>
    /// Berechnet und verbucht Offline-Einnahmen seit dem letzten Speichern.
    /// Liefert (Dauer, Einnahmen) für die Anzeige, oder null bei Bagatellzeiten.
    /// </summary>
    public (TimeSpan Duration, double Earned)? ApplyOfflineProgress()
    {
        var elapsed = DateTime.UtcNow - State.LastSavedUtc;
        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return null;
        }

        var counted = elapsed > OfflineCap ? OfflineCap : elapsed;
        var earned = TotalProductionPerSecond() * counted.TotalSeconds * OfflineEfficiency;
        if (earned <= 0)
        {
            return null;
        }

        AddStempel(earned);
        Log.Info("Offline-Fortschritt: {Duration} abwesend, +{Earned:0} Stempel", counted, earned);
        return (counted, earned);
    }

    // ---------- Achievements ----------

    private void CheckAchievements()
    {
        foreach (var achievement in GameDefinitions.Achievements)
        {
            if (!State.UnlockedAchievements.Contains(achievement.Id) && achievement.Condition(State))
            {
                State.UnlockedAchievements.Add(achievement.Id);
                Log.Info("Achievement freigeschaltet: {Name}", achievement.Name);
                AchievementUnlocked?.Invoke(achievement);
            }
        }
    }
}
