namespace Amtsschimmel.Models;

/// <summary>Wirkungsart einer Fortbildung.</summary>
public enum ResearchEffectType
{
    /// <summary>Multipliziert die Produktion bestimmter Generatoren (Targets), je Stufe.</summary>
    GeneratorMultiplier,

    /// <summary>Multipliziert die Produktion aller Generatoren, je Stufe.</summary>
    AllGeneratorsMultiplier,

    /// <summary>Multipliziert die Klickkraft, je Stufe.</summary>
    ClickMultiplier,

    /// <summary>Reduziert alle Generatorkosten um den Faktor je Stufe (0,05 = −5 %).</summary>
    CostReduction,

    /// <summary>Setzt die Offline-Effizienz auf den Wert (nicht stufbar).</summary>
    OfflineEfficiency,

    /// <summary>Setzt das Offline-Cap auf den Wert in Stunden (nicht stufbar).</summary>
    OfflineCapHours,

    /// <summary>Erhöht den Paragraphen-Ertrag bei Reformen um den Faktor je Stufe (0,25 = +25 %).</summary>
    ParagraphBonus,

    /// <summary>Automatisches Stempeln: Wert = Klicks pro Sekunde je Stufe (additiv).</summary>
    AutoClick,
}

/// <summary>
/// Definition einer Fortbildung. <paramref name="MaxLevel"/>: 1 = einmalig,
/// n = bis Stufe n wiederholbar, 0 = endlos wiederholbar.
/// </summary>
public sealed record ResearchDefinition(
    string Id,
    string Name,
    string Description,
    double Cost,
    ResearchEffectType EffectType,
    double Value,
    string[]? TargetGeneratorIds = null,
    string[]? Prerequisites = null,
    int MaxLevel = 1,
    double CostGrowth = 8,
    int MinReformen = 0)
{
    public bool IsEndless => MaxLevel == 0;
    public bool IsRepeatable => MaxLevel != 1;

    /// <summary>Kosten für die nächste Stufe bei aktuellem Level.</summary>
    public double CostForLevel(int currentLevel) => Cost * Math.Pow(CostGrowth, currentLevel);

    /// <summary>true, wenn bei diesem Level keine weitere Stufe mehr möglich ist.</summary>
    public bool IsMaxed(int currentLevel) => !IsEndless && currentLevel >= MaxLevel;

    /// <summary>Menschenlesbare Effektbeschreibung für die UI.</summary>
    public string EffectText
    {
        get
        {
            var perLevel = IsRepeatable ? " je Stufe" : "";
            return EffectType switch
            {
                ResearchEffectType.GeneratorMultiplier => $"×{Value:0.##} Produktion{perLevel}: {TargetNames()}",
                ResearchEffectType.AllGeneratorsMultiplier => $"×{Value:0.##} Produktion (alle){perLevel}",
                ResearchEffectType.ClickMultiplier => $"×{Value:0.##} Klickkraft{perLevel}",
                ResearchEffectType.CostReduction => $"−{Value:P0} Generatorkosten{perLevel}",
                ResearchEffectType.OfflineEfficiency => $"Offline-Effizienz auf {Value:P0}",
                ResearchEffectType.OfflineCapHours => $"Offline-Limit auf {Value:0} h",
                ResearchEffectType.ParagraphBonus => $"+{Value:P0} Paragraphen bei Reformen{perLevel}",
                ResearchEffectType.AutoClick => $"+{Value:0.#} Auto-Stempel/s{perLevel}",
                _ => "",
            };
        }
    }

    private string TargetNames() => string.Join(", ",
        (TargetGeneratorIds ?? []).Select(id =>
            GameDefinitions.Generators.FirstOrDefault(g => g.Id == id)?.Name ?? id));
}

/// <summary>Der Forschungsbaum der Verwaltungsakademie.</summary>
public static class ResearchDefinitions
{
    public static readonly IReadOnlyList<ResearchDefinition> All =
    [
        // ---- Stufe 1: Grundkurse ----
        new("zehnfinger", "Zehnfingersystem",
            "Ein VHS-Kurs, der die Stempelhand revolutioniert. Auffrischung jederzeit buchbar.",
            500, ResearchEffectType.ClickMultiplier, 2,
            MaxLevel: 5),

        new("kaffeekueche", "Kaffeeküchen-Erlass",
            "Koffeinversorgung wird Dienstpflicht. Ausbaustufen: Filter, Vollautomat, Barista.",
            2_500, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["praktikant", "sachbearbeiter"],
            MaxLevel: 3, CostGrowth: 10),

        new("ordner", "Ordnerbeschriftungs-Norm",
            "Einheitliche Rückenschilder. Jede Novelle macht alles 25 % runder.",
            10_000, ResearchEffectType.AllGeneratorsMultiplier, 1.25,
            MaxLevel: 5, CostGrowth: 10),

        // ---- Stufe 2: Aufbaukurse ----
        new("stempelhalter", "Ergonomische Stempelhalter",
            "Beschafft nach nur drei Ausschreibungsrunden.",
            50_000, ResearchEffectType.ClickMultiplier, 3,
            Prerequisites: ["zehnfinger"]),

        new("flurfunk", "Flurfunk 2.0",
            "Informationsfluss per strukturiertem Tratsch. Führungskräfte je Stufe doppelt so wirksam.",
            250_000, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["teamleiter", "amtsleiter"],
            Prerequisites: ["kaffeekueche"],
            MaxLevel: 3, CostGrowth: 10),

        new("sammelbestellung", "Sammelbestellungs-Richtlinie",
            "Rabatt durch Rahmenverträge. Jede Neuverhandlung spart weitere 5 %.",
            1e6, ResearchEffectType.CostReduction, 0.05,
            Prerequisites: ["ordner"],
            MaxLevel: 5, CostGrowth: 12),

        new("gleitzeit", "Gleitzeitmodell",
            "Das Amt arbeitet weiter, auch wenn du nicht hinschaust — jetzt mit 75 % Effizienz.",
            5e6, ResearchEffectType.OfflineEfficiency, 0.75,
            Prerequisites: ["ordner"]),

        new("stempelautomat", "Pneumatischer Stempelautomat",
            "Ein Wunderwerk der Bürotechnik: stempelt selbstständig, klemmt nur selten.",
            2e6, ResearchEffectType.AutoClick, 1,
            Prerequisites: ["stempelhalter"],
            MaxLevel: 10, CostGrowth: 6,
            MinReformen: 1),

        // ---- Stufe 3: Fachfortbildungen ----
        new("digitale_akte", "Digitale Akte",
            "Nur 20 Jahre nach Ankündigung. Produktivität +50 % (alle).",
            1e7, ResearchEffectType.AllGeneratorsMultiplier, 1.5,
            Prerequisites: ["flurfunk", "sammelbestellung"]),

        new("homeoffice", "Homeoffice-Verordnung",
            "Der Dienstlaptop darf mit nach Hause. Offline-Limit steigt auf 24 h.",
            5e7, ResearchEffectType.OfflineCapHours, 24,
            Prerequisites: ["gleitzeit"]),

        new("beschaffung", "Zentrale Beschaffungsstelle",
            "Ein Amt nur fürs Einkaufen. Je Ausbaustufe weitere 10 % Kostenersparnis.",
            1e8, ResearchEffectType.CostReduction, 0.10,
            Prerequisites: ["sammelbestellung"],
            MaxLevel: 3, CostGrowth: 15),

        new("matrixorg", "Matrixorganisation",
            "Fachbereiche und Dezernate melden alles doppelt. Fördert die Verantwortungsdiffusion.",
            5e6, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["fachbereich", "dezernat"],
            Prerequisites: ["flurfunk"],
            MaxLevel: 3, CostGrowth: 10,
            MinReformen: 1),

        new("reformkommission", "Reformkommission",
            "Ein Gremium, das Reformen vorbereitet: je Sitzungsperiode +25 % Paragraphen.",
            5e8, ResearchEffectType.ParagraphBonus, 0.25,
            Prerequisites: ["digitale_akte"],
            MaxLevel: 3, CostGrowth: 20),

        // ---- Stufe 4: Exzellenzinitiative ----
        new("lean_admin", "Lean Administration",
            "Ein Unternehmensberater hat 'Prozesse verschlankt'. Verdoppelt trotzdem alles.",
            1e9, ResearchEffectType.AllGeneratorsMultiplier, 2,
            Prerequisites: ["digitale_akte"],
            MinReformen: 1),

        new("ki_sachbearbeitung", "KI-Sachbearbeitung",
            "Die Cloud stempelt jetzt eigenverantwortlich. §35a VwVfG lässt grüßen.",
            1e10, ResearchEffectType.GeneratorMultiplier, 3,
            TargetGeneratorIds: ["ministerium", "ki_cloud"],
            Prerequisites: ["lean_admin"],
            MinReformen: 2),

        new("foederalismus", "Föderalismusreform",
            "Ein Kompromiss zwischen Bund, Ländern und Kommunen. Rathäuser und Landesbehörden stempeln jetzt widerspruchsfrei.",
            5e8, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["rathaus", "landesbehoerde"],
            Prerequisites: ["matrixorg"],
            MaxLevel: 3, CostGrowth: 10,
            MinReformen: 2),

        new("verwaltungsexzellenz", "Verwaltungsexzellenz-Cluster",
            "Das Amt wird Forschungsstandort. +50 % Paragraphen bei Reformen.",
            5e10, ResearchEffectType.ParagraphBonus, 0.50,
            Prerequisites: ["reformkommission", "lean_admin"],
            MinReformen: 2),

        new("buerokratieabbau", "Bürokratieabbau (Endlosvorhaben)",
            "Seit Jahrzehnten in Arbeit, nie abgeschlossen — aber jede Stufe bringt 10 % mehr Output.",
            1e11, ResearchEffectType.AllGeneratorsMultiplier, 1.1,
            Prerequisites: ["lean_admin"],
            MaxLevel: 0, CostGrowth: 10,
            MinReformen: 3),
    ];
}
