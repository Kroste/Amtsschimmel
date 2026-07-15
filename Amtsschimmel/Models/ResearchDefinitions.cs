namespace Amtsschimmel.Models;

/// <summary>Wirkungsart einer Fortbildung.</summary>
public enum ResearchEffectType
{
    /// <summary>Multipliziert die Produktion bestimmter Generatoren (Targets).</summary>
    GeneratorMultiplier,

    /// <summary>Multipliziert die Produktion aller Generatoren.</summary>
    AllGeneratorsMultiplier,

    /// <summary>Multipliziert die Klickkraft.</summary>
    ClickMultiplier,

    /// <summary>Reduziert alle Generatorkosten um den Faktor (0,05 = −5 %).</summary>
    CostReduction,

    /// <summary>Setzt die Offline-Effizienz auf den Wert (z. B. 0,75).</summary>
    OfflineEfficiency,

    /// <summary>Setzt das Offline-Cap auf den Wert in Stunden (z. B. 24).</summary>
    OfflineCapHours,

    /// <summary>Erhöht den Paragraphen-Ertrag bei Reformen um den Faktor (0,25 = +25 %).</summary>
    ParagraphBonus,
}

/// <summary>Definition einer Fortbildung in der Verwaltungsakademie.</summary>
public sealed record ResearchDefinition(
    string Id,
    string Name,
    string Description,
    double Cost,
    ResearchEffectType EffectType,
    double Value,
    string[]? TargetGeneratorIds = null,
    string[]? Prerequisites = null)
{
    /// <summary>Menschenlesbare Effektbeschreibung für die UI.</summary>
    public string EffectText => EffectType switch
    {
        ResearchEffectType.GeneratorMultiplier => $"×{Value:0.#} Produktion: {TargetNames()}",
        ResearchEffectType.AllGeneratorsMultiplier => $"×{Value:0.#} Produktion (alle)",
        ResearchEffectType.ClickMultiplier => $"×{Value:0.#} Klickkraft",
        ResearchEffectType.CostReduction => $"−{Value:P0} Generatorkosten",
        ResearchEffectType.OfflineEfficiency => $"Offline-Effizienz auf {Value:P0}",
        ResearchEffectType.OfflineCapHours => $"Offline-Limit auf {Value:0} h",
        ResearchEffectType.ParagraphBonus => $"+{Value:P0} Paragraphen bei Reformen",
        _ => "",
    };

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
            "Ein VHS-Kurs, der die Stempelhand revolutioniert.",
            500, ResearchEffectType.ClickMultiplier, 2),

        new("kaffeekueche", "Kaffeeküchen-Erlass",
            "Koffeinversorgung wird Dienstpflicht. Die Basis jubelt.",
            2_500, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["praktikant", "sachbearbeiter"]),

        new("ordner", "Ordnerbeschriftungs-Norm",
            "Endlich einheitliche Rückenschilder. Alles läuft 25 % runder.",
            10_000, ResearchEffectType.AllGeneratorsMultiplier, 1.25),

        // ---- Stufe 2: Aufbaukurse ----
        new("stempelhalter", "Ergonomische Stempelhalter",
            "Beschafft nach nur drei Ausschreibungsrunden.",
            50_000, ResearchEffectType.ClickMultiplier, 3,
            Prerequisites: ["zehnfinger"]),

        new("flurfunk", "Flurfunk 2.0",
            "Informationsfluss per strukturiertem Tratsch. Führungskräfte doppelt so wirksam.",
            250_000, ResearchEffectType.GeneratorMultiplier, 2,
            TargetGeneratorIds: ["teamleiter", "amtsleiter"],
            Prerequisites: ["kaffeekueche"]),

        new("sammelbestellung", "Sammelbestellungs-Richtlinie",
            "Rabatt durch Rahmenverträge: alle Einstellungen 5 % günstiger.",
            1e6, ResearchEffectType.CostReduction, 0.05,
            Prerequisites: ["ordner"]),

        new("gleitzeit", "Gleitzeitmodell",
            "Das Amt arbeitet weiter, auch wenn du nicht hinschaust — jetzt mit 75 % Effizienz.",
            5e6, ResearchEffectType.OfflineEfficiency, 0.75,
            Prerequisites: ["ordner"]),

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
            "Ein Amt nur fürs Einkaufen. Weitere 10 % Kostenersparnis.",
            1e8, ResearchEffectType.CostReduction, 0.10,
            Prerequisites: ["sammelbestellung"]),

        new("reformkommission", "Reformkommission",
            "Ein Gremium, das Reformen vorbereitet: +25 % Paragraphen bei jeder Reform.",
            5e8, ResearchEffectType.ParagraphBonus, 0.25,
            Prerequisites: ["digitale_akte"]),

        // ---- Stufe 4: Exzellenzinitiative ----
        new("lean_admin", "Lean Administration",
            "Ein Unternehmensberater hat 'Prozesse verschlankt'. Verdoppelt trotzdem alles.",
            1e9, ResearchEffectType.AllGeneratorsMultiplier, 2,
            Prerequisites: ["digitale_akte"]),

        new("ki_sachbearbeitung", "KI-Sachbearbeitung",
            "Die Cloud stempelt jetzt eigenverantwortlich. §35a VwVfG lässt grüßen.",
            1e10, ResearchEffectType.GeneratorMultiplier, 3,
            TargetGeneratorIds: ["ministerium", "ki_cloud"],
            Prerequisites: ["lean_admin"]),

        new("verwaltungsexzellenz", "Verwaltungsexzellenz-Cluster",
            "Das Amt wird Forschungsstandort. +50 % Paragraphen bei Reformen.",
            5e10, ResearchEffectType.ParagraphBonus, 0.50,
            Prerequisites: ["reformkommission", "lean_admin"]),
    ];
}
