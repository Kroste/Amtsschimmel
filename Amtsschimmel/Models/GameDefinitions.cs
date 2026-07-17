namespace Amtsschimmel.Models;

/// <summary>Unveränderliche Definition eines Generators (Personal/Einrichtung).</summary>
public sealed record GeneratorDefinition(
    string Id,
    string Name,
    string Description,
    double BaseCost,
    double BaseProduction,
    int MinReformen = 0)
{
    /// <summary>Kostenwachstum pro gekaufter Einheit (Cookie-Clicker-Standard).</summary>
    public const double CostGrowth = 1.15;

    /// <summary>Preis für die nächste Einheit bei <paramref name="owned"/> vorhandenen.</summary>
    public double CostFor(int owned) => BaseCost * Math.Pow(CostGrowth, owned);

    /// <summary>Summenpreis für <paramref name="amount"/> Einheiten ab Bestand <paramref name="owned"/> (geometrische Reihe).</summary>
    public double CostForBulk(int owned, int amount)
        => BaseCost * Math.Pow(CostGrowth, owned) * (Math.Pow(CostGrowth, amount) - 1) / (CostGrowth - 1);

    /// <summary>Preis des Auto-Buyers für diesen Generator.</summary>
    public double AutoBuyerCost => BaseCost * 250;
}

/// <summary>Definition eines Achievements inkl. Freischaltbedingung.</summary>
public sealed record AchievementDefinition(
    string Id,
    string Name,
    string Description,
    Func<GameState, bool> Condition);

/// <summary>Zentrale statische Spieldaten.</summary>
public static class GameDefinitions
{
    /// <summary>
    /// Meilenstein-Schwellen ("Beförderungen"): je ×2 Produktion, die letzte Stufe (250)
    /// ist die Endbeförderung mit ×3. Danach ist Schluss — der Stellenplan ist erfüllt.
    /// </summary>
    public static readonly long[] MilestoneThresholds = [10, 25, 50, 100, 175, 250];

    /// <summary>Letzte Beförderungsstufe (Endbeförderung, ×3).</summary>
    public const long FinalMilestone = 250;

    public static readonly IReadOnlyList<GeneratorDefinition> Generators =
    [
        new("praktikant",    "Praktikant",            "Stempelt langsam, aber kostenlos motiviert.",              15,        0.1),
        new("sachbearbeiter","Sachbearbeiter",        "Das Rückgrat der Verwaltung. Kaffee inklusive.",           100,       1),
        new("teamleiter",    "Teamleiter",            "Delegiert Stempelvorgänge mit beeindruckender Effizienz.", 1_100,     8),
        new("amtsleiter",    "Amtsleiter",            "Unterschreibt, was der Teamleiter delegiert hat.",         12_000,    47),
        new("fachbereich",   "Fachbereich",           "Eine ganze Etage voller Aktenordner und Tatendrang.",      130_000,   260),
        new("dezernat",      "Dezernat",              "Koordiniert Fachbereiche. Niemand weiß, wie.",             1.4e6,     1_400),
        new("rathaus",       "Rathaus",               "Das Herz der Stadt. Schlägt in Dreifachausfertigung.",     2.0e7,     7_800),
        new("landesbehoerde","Landesbehörde",         "Stempelt jetzt auch landesweit einheitlich. Fast.",        3.3e8,     44_000),
        new("ministerium",   "Bundesministerium",     "Erlässt Verordnungen zur Stempeloptimierung.",             5.1e9,     260_000,  MinReformen: 1),
        new("ki_cloud",      "KI-Verwaltungscloud",   "Stempelt digital. Der Amtsschimmel wiehert elektrisch.",   7.5e10,    1.6e6,    MinReformen: 2),
    ];

    public static readonly IReadOnlyList<AchievementDefinition> Achievements =
    [
        new("first_click",   "Dienstbeginn",          "Stemple dein erstes Formular.",                 s => s.TotalClicks >= 1),
        new("clicks_100",    "Sehnenscheide ade",     "100 manuelle Stempelvorgänge.",                 s => s.TotalClicks >= 100),
        new("clicks_10k",    "Handstempel-Held",      "10.000 manuelle Stempelvorgänge.",              s => s.TotalClicks >= 10_000),
        new("earn_1k",       "Kleines Amt",           "1.000 Stempel insgesamt verdient.",             s => s.TotalEarnedAllTime >= 1_000),
        new("earn_1m",       "Stempelmillionär",      "1 Million Stempel insgesamt verdient.",         s => s.TotalEarnedAllTime >= 1e6),
        new("earn_1b",       "Bürokratie-Baron",      "1 Milliarde Stempel insgesamt verdient.",       s => s.TotalEarnedAllTime >= 1e9),
        new("earn_1t",       "Formular-Fürst",        "1 Billion Stempel insgesamt verdient.",         s => s.TotalEarnedAllTime >= 1e12),
        new("gen_first",     "Einstellungszusage",    "Stelle deinen ersten Mitarbeiter ein.",         s => TotalOwned(s) >= 1),
        new("gen_50",        "Personalrat gegründet", "50 Einheiten insgesamt beschäftigt.",           s => TotalOwned(s) >= 50),
        new("gen_200",       "Stellenplan gesprengt", "200 Einheiten insgesamt beschäftigt.",          s => TotalOwned(s) >= 200),
        new("gen_all",       "Volle Besetzung",       "Besitze jeden Generatortyp mindestens einmal.", s => GameDefinitions.Generators.All(g => s.GetGenerator(g.Id).Owned >= 1)),
        new("auto_first",    "Prozessoptimierung",    "Kaufe deinen ersten Auto-Buyer.",               s => s.Generators.Values.Any(g => g.AutoBuyerOwned)),
        new("auto_all",      "Vollautomatisiert",     "Kaufe alle Auto-Buyer.",                        s => GameDefinitions.Generators.All(g => s.GetGenerator(g.Id).AutoBuyerOwned)),
        new("reform_first",  "Verwaltungsreform",     "Führe deine erste Reform durch.",               s => s.TotalReformen >= 1),
        new("reform_5",      "Reformstau gelöst",     "5 Verwaltungsreformen durchgeführt.",           s => s.TotalReformen >= 5),
        new("research_first","Bildungsauftrag",       "Schließe deine erste Fortbildung ab.",          s => s.ResearchLevels.Count >= 1),
        new("research_all",  "Summa cum laude",       "Erforsche jede Fortbildung mindestens einmal.", s => ResearchDefinitions.All.All(r => s.GetResearchLevel(r.Id) >= 1)),
        new("research_lvl25","Lebenslanges Lernen",   "Erreiche 25 Fortbildungsstufen insgesamt.",     s => s.ResearchLevels.Values.Sum() >= 25),
        new("para_100",      "Grundgesetz 2.0",       "Sammle 100 Paragraphen.",                       s => s.Paragraphen >= 100),
        new("click_lvl_5",   "Turbo-Stempelkissen",   "Klick-Upgrade auf Stufe 5.",                    s => s.ClickUpgradeLevel >= 5),
        new("gen_100_single","Beförderungswelle",     "Beschäftige 100 Einheiten eines Generatortyps.",s => s.Generators.Values.Any(g => g.Owned >= 100)),
        new("golden_10",     "Goldgräber",            "Erwische 10 Goldene Formulare.",                s => s.GoldenFormsClicked >= 10),
        new("gen_250",       "Stellenplan erfüllt",   "250 Einheiten eines Generatortyps — Endbeförderung!", s => s.Generators.Values.Any(g => g.Owned >= 250)),
        new("victory",       "Der Amtsschimmel",      "Erwirb den Goldenen Aktendeckel — das Amt ist vollendet.", s => s.HasWon),
    ];

    private static int TotalOwned(GameState s) => s.Generators.Values.Sum(g => g.Owned);
}
