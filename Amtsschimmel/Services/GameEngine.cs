using Amtsschimmel.Models;
using NLog;

namespace Amtsschimmel.Services;

/// <summary>
/// UI-unabhängige Spiellogik: Produktion, Käufe, Forschung, Prestige,
/// Auto-Buyer, Offline-Fortschritt. Wird vom ViewModel per Timer getickt.
/// </summary>
public sealed class GameEngine
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Basis-Schwelle für die erste Reform.</summary>
    public const double BasePrestigeThreshold = 1e6;

    /// <summary>Faktor, um den die Reform-Schwelle mit jeder Reform wächst.</summary>
    public const double PrestigeThresholdGrowth = 10;

    private static readonly TimeSpan BaseOfflineCap = TimeSpan.FromHours(8);
    private const double BaseOfflineEfficiency = 0.5;

    public GameState State { get; private set; } = new();

    /// <summary>Wird gefeuert, wenn ein Achievement neu freigeschaltet wurde.</summary>
    public event Action<AchievementDefinition>? AchievementUnlocked;

    /// <summary>Wird gefeuert, wenn ein Generator eine Meilenstein-Schwelle überschreitet.</summary>
    public event Action<GeneratorDefinition, long>? MilestoneReached;

    // ---------- Temporärer Produktions-Buff (Goldene Formulare) ----------

    private double _buffFactor = 1;
    private DateTime _buffExpiresUtc = DateTime.MinValue;

    public bool IsBuffActive => DateTime.UtcNow < _buffExpiresUtc;

    /// <summary>Aktiver Buff-Faktor (1, wenn kein Buff läuft).</summary>
    public double ActiveBuffFactor => IsBuffActive ? _buffFactor : 1;

    public TimeSpan BuffRemaining => IsBuffActive ? _buffExpiresUtc - DateTime.UtcNow : TimeSpan.Zero;

    public void ActivateProductionBuff(double factor, TimeSpan duration)
    {
        _buffFactor = factor;
        _buffExpiresUtc = DateTime.UtcNow + duration;
        Log.Info("Produktions-Buff aktiv: ×{Factor} für {Duration}", factor, duration);
    }

    /// <summary>
    /// Belohnung für ein geklicktes Goldenes Formular: entweder Sofort-Stempel
    /// ("Fördermittel") oder temporärer Produktions-Buff ("Erlassflut").
    /// </summary>
    public string GrantGoldenFormReward(Random rng)
    {
        State.GoldenFormsClicked++;
        if (rng.Next(2) == 0)
        {
            var amount = Math.Max(500, EffectiveIncomePerSecond() * 90 + State.Stempel * 0.05);
            AddStempel(amount);
            Log.Info("Goldenes Formular: Fördermittel +{Amount:0}", amount);
            return $"💰 Fördermittel bewilligt: +{NumberFormatter.Format(amount)} Stempel!";
        }
        ActivateProductionBuff(7, TimeSpan.FromSeconds(30));
        return "⚡ Erlassflut! ×7 Produktion für 30 Sekunden!";
    }

    // ---------- Meilensteine ("Beförderungen") ----------

    /// <summary>×2 je erreichter Meilenstein-Schwelle (endlose Folge 10/25/50/100/250/…).</summary>
    public double MilestoneMultiplierFor(string generatorId)
    {
        var owned = State.GetGenerator(generatorId).Owned;
        var reached = GameDefinitions.MilestoneSequence().TakeWhile(t => t <= owned).Count();
        return Math.Pow(2, reached);
    }

    /// <summary>Nächste noch nicht erreichte Meilenstein-Schwelle (Folge ist endlos).</summary>
    public long NextMilestoneFor(string generatorId)
    {
        var owned = State.GetGenerator(generatorId).Owned;
        return GameDefinitions.MilestoneSequence().First(t => t > owned);
    }

    public void LoadState(GameState state)
    {
        State = state;
        MigrateLegacyResearch();
    }

    /// <summary>Migriert einstufige Forschung (≤ v1.1.0, ResearchedIds) auf das Level-System.</summary>
    private void MigrateLegacyResearch()
    {
        foreach (var id in State.ResearchedIds)
        {
            if (State.GetResearchLevel(id) < 1)
            {
                State.ResearchLevels[id] = 1;
            }
        }
        State.ResearchedIds.Clear();
    }

    // ---------- Forschung (Verwaltungsakademie) ----------

    /// <summary>Alle Fortbildungen mit Level ≥ 1, gepaart mit ihrem Level.</summary>
    private IEnumerable<(ResearchDefinition Def, int Level)> ActiveResearch =>
        ResearchDefinitions.All
            .Select(r => (Def: r, Level: State.GetResearchLevel(r.Id)))
            .Where(x => x.Level > 0);

    public int ResearchLevel(ResearchDefinition def) => State.GetResearchLevel(def.Id);

    /// <summary>true, wenn keine weitere Stufe mehr möglich ist.</summary>
    public bool IsMaxed(ResearchDefinition def) => def.IsMaxed(ResearchLevel(def));

    /// <summary>Alle Voraussetzungen (Level ≥ 1) erfüllt?</summary>
    public bool PrerequisitesMet(ResearchDefinition def) =>
        (def.Prerequisites ?? []).All(id => State.GetResearchLevel(id) >= 1);

    /// <summary>Kosten der nächsten Stufe.</summary>
    public double NextResearchCost(ResearchDefinition def) =>
        def.CostForLevel(ResearchLevel(def));

    public bool CanResearch(ResearchDefinition def) =>
        !IsMaxed(def) && PrerequisitesMet(def)
        && ReformRequirementMet(def.MinReformen)
        && CanAfford(NextResearchCost(def));

    public bool BuyResearch(ResearchDefinition def)
    {
        if (!CanResearch(def))
        {
            return false;
        }
        State.Stempel -= NextResearchCost(def);
        var level = ResearchLevel(def) + 1;
        State.ResearchLevels[def.Id] = level;
        Log.Info("Fortbildung: {Name} auf Stufe {Level} ({Effect})", def.Name, level, def.EffectText);
        return true;
    }

    /// <summary>Forschungs-Multiplikator für einen konkreten Generator (Effekte stapeln je Stufe).</summary>
    public double ResearchMultiplierFor(string generatorId)
    {
        var factor = 1.0;
        foreach (var (research, level) in ActiveResearch)
        {
            factor *= research.EffectType switch
            {
                ResearchEffectType.AllGeneratorsMultiplier => Math.Pow(research.Value, level),
                ResearchEffectType.GeneratorMultiplier
                    when research.TargetGeneratorIds?.Contains(generatorId) == true
                    => Math.Pow(research.Value, level),
                _ => 1.0,
            };
        }
        return factor;
    }

    private double ResearchClickMultiplier => ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.ClickMultiplier)
        .Aggregate(1.0, (acc, x) => acc * Math.Pow(x.Def.Value, x.Level));

    /// <summary>Kostenfaktor aus Rabatt-Fortbildungen ((1 − v)^Stufe, über Knoten multipliziert).</summary>
    public double CostFactor => ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.CostReduction)
        .Aggregate(1.0, (acc, x) => acc * Math.Pow(1.0 - x.Def.Value, x.Level));

    /// <summary>Aktuelle Offline-Effizienz (Basis 50 %, per Forschung erhöhbar).</summary>
    public double OfflineEfficiency => ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.OfflineEfficiency)
        .Select(x => x.Def.Value)
        .DefaultIfEmpty(BaseOfflineEfficiency)
        .Max();

    /// <summary>Aktuelles Offline-Cap (Basis 8 h, per Forschung erhöhbar).</summary>
    public TimeSpan OfflineCap => TimeSpan.FromHours(ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.OfflineCapHours)
        .Select(x => x.Def.Value)
        .DefaultIfEmpty(BaseOfflineCap.TotalHours)
        .Max());

    /// <summary>Multiplikator auf den Paragraphen-Ertrag bei Reformen ((1 + v)^Stufe).</summary>
    public double ParagraphMultiplier => ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.ParagraphBonus)
        .Aggregate(1.0, (acc, x) => acc * Math.Pow(1.0 + x.Def.Value, x.Level));

    // ---------- Produktion ----------

    /// <summary>Globaler Multiplikator aus Paragraphen (+5 % je) und Achievements (+1 % je).</summary>
    public double GlobalMultiplier =>
        (1.0 + State.Paragraphen * 0.05) * (1.0 + State.UnlockedAchievements.Count * 0.01);

    /// <summary>Produktion eines einzelnen Generators pro Sekunde (inkl. aller Multiplikatoren).</summary>
    public double ProductionPerSecond(GeneratorDefinition def) =>
        def.BaseProduction * State.GetGenerator(def.Id).Owned
        * GlobalMultiplier * ResearchMultiplierFor(def.Id)
        * MilestoneMultiplierFor(def.Id) * ActiveBuffFactor;

    /// <summary>Gesamtproduktion pro Sekunde.</summary>
    public double TotalProductionPerSecond() =>
        GameDefinitions.Generators.Sum(ProductionPerSecond);

    /// <summary>Klickkraft: 2^Upgrade-Stufe × globaler Multiplikator × Klick-Forschung.</summary>
    public double ClickPower =>
        Math.Pow(2, State.ClickUpgradeLevel) * GlobalMultiplier * ResearchClickMultiplier;

    /// <summary>Kosten des nächsten Klick-Upgrades ("Stempelkissen").</summary>
    public double ClickUpgradeCost => 100 * Math.Pow(12, State.ClickUpgradeLevel);

    /// <summary>Automatische Klicks pro Sekunde aus Forschung (additiv je Stufe).</summary>
    public double AutoClicksPerSecond => ActiveResearch
        .Where(x => x.Def.EffectType == ResearchEffectType.AutoClick)
        .Sum(x => x.Def.Value * x.Level);

    /// <summary>Gesamteinkommen pro Sekunde: Generatoren + Auto-Stempeln.</summary>
    public double EffectiveIncomePerSecond() =>
        TotalProductionPerSecond() + ClickPower * AutoClicksPerSecond;

    // ---------- Effektive Kosten (inkl. Forschungsrabatte) ----------

    public double NextCost(GeneratorDefinition def) =>
        def.CostFor(State.GetGenerator(def.Id).Owned) * CostFactor;

    public double BulkCost(GeneratorDefinition def, int amount) =>
        def.CostForBulk(State.GetGenerator(def.Id).Owned, amount) * CostFactor;

    // ---------- Tick ----------

    /// <summary>Ein Simulationsschritt. <paramref name="deltaSeconds"/> = vergangene Zeit.</summary>
    public void Tick(double deltaSeconds)
    {
        State.TotalPlaySeconds += deltaSeconds;
        var income = EffectiveIncomePerSecond();
        if (income > State.HighestIncomePerSec)
        {
            State.HighestIncomePerSec = income;
        }
        var gain = income * deltaSeconds;
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

    /// <summary>true, wenn genug Reformen für die Freischaltung durchgeführt wurden.</summary>
    public bool ReformRequirementMet(int minReformen) => State.TotalReformen >= minReformen;

    /// <summary>Kauft <paramref name="amount"/> Einheiten eines Generators, falls bezahlbar.</summary>
    public bool BuyGenerator(GeneratorDefinition def, int amount = 1)
    {
        if (!ReformRequirementMet(def.MinReformen))
        {
            return false;
        }
        var gen = State.GetGenerator(def.Id);
        var cost = BulkCost(def, amount);
        if (!CanAfford(cost))
        {
            return false;
        }
        State.Stempel -= cost;
        var before = gen.Owned;
        gen.Owned += amount;
        Log.Debug("Gekauft: {Amount}x {Name} für {Cost:0} Stempel", amount, def.Name, cost);
        foreach (var threshold in GameDefinitions.MilestoneSequence()
                     .SkipWhile(t => t <= before)
                     .TakeWhile(t => t <= gen.Owned))
        {
            Log.Info("Meilenstein: {Name} erreicht {Threshold} Einheiten (×2)", def.Name, threshold);
            MilestoneReached?.Invoke(def, threshold);
        }
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
            if (gen.AutoBuyerOwned && gen.AutoBuyerEnabled && CanAfford(NextCost(def)))
            {
                BuyGenerator(def);
            }
        }
    }

    // ---------- Prestige ----------

    /// <summary>Aktuelle Reform-Schwelle: wächst mit jeder Reform um Faktor 10.</summary>
    public double CurrentPrestigeThreshold =>
        BasePrestigeThreshold * Math.Pow(PrestigeThresholdGrowth, State.TotalReformen);

    /// <summary>Paragraphen, die eine Reform jetzt einbringen würde (inkl. Forschungsbonus).</summary>
    public int PendingParagraphen =>
        State.TotalEarnedThisRun < CurrentPrestigeThreshold
            ? 0
            : (int)Math.Floor(Math.Sqrt(State.TotalEarnedThisRun / CurrentPrestigeThreshold) * ParagraphMultiplier);

    public bool CanPrestige => PendingParagraphen > 0;

    /// <summary>
    /// Verwaltungsreform: setzt Stempel, Generatoren, Klick-Upgrade und Forschung zurück,
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
        State.ResearchLevels.Clear(); // Die Reform reformiert auch die Fortbildungslandschaft.
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

        var cap = OfflineCap;
        var counted = elapsed > cap ? cap : elapsed;
        var earned = EffectiveIncomePerSecond() * counted.TotalSeconds * OfflineEfficiency;
        if (earned <= 0)
        {
            return null;
        }

        AddStempel(earned);
        Log.Info("Offline-Fortschritt: {Duration} abwesend, +{Earned:0} Stempel", counted, earned);
        return (counted, earned);
    }

    // ---------- Siegesbedingung ("Verwaltungsvollendung") ----------

    /// <summary>Preis des Goldenen Aktendeckels — das finale Kaufziel.</summary>
    public const double VictoryCost = 1e18;

    /// <summary>Mindestanzahl Reformen für die Vollendung.</summary>
    public const int VictoryMinReformen = 25;

    /// <summary>Alle Fortbildungen mindestens einmal erforscht?</summary>
    public bool AllResearchCompleted =>
        ResearchDefinitions.All.All(r => State.GetResearchLevel(r.Id) >= 1);

    public bool VictoryReformsMet => State.TotalReformen >= VictoryMinReformen;

    /// <summary>Alle Bedingungen erfüllt UND bezahlbar?</summary>
    public bool CanWin =>
        !State.HasWon && AllResearchCompleted && VictoryReformsMet && CanAfford(VictoryCost);

    /// <summary>
    /// Kauft den Goldenen Aktendeckel: markiert den Spielstand als gewonnen.
    /// Das Spiel läuft danach als Endlosmodus weiter.
    /// </summary>
    public bool Win()
    {
        if (!CanWin)
        {
            return false;
        }
        State.Stempel -= VictoryCost;
        State.HasWon = true;
        State.WonAtUtc = DateTime.UtcNow;
        State.WonAfterPlaySeconds = State.TotalPlaySeconds;
        Log.Info("SIEG: Goldener Aktendeckel erworben nach {Reformen} Reformen und {Zeit:0} s Spielzeit.",
            State.TotalReformen, State.TotalPlaySeconds);
        return true;
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
