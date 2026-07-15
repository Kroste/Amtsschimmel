namespace Amtsschimmel.Models;

/// <summary>
/// Kompletter persistierbarer Spielzustand. Wird als JSON gespeichert.
/// </summary>
public sealed class GameState
{
    /// <summary>Aktueller Kontostand an Stempeln.</summary>
    public double Stempel { get; set; }

    /// <summary>In diesem Durchlauf insgesamt verdiente Stempel (Basis für Prestige).</summary>
    public double TotalEarnedThisRun { get; set; }

    /// <summary>Über alle Durchläufe verdiente Stempel (für Achievements).</summary>
    public double TotalEarnedAllTime { get; set; }

    /// <summary>Anzahl manueller Klicks (alle Durchläufe).</summary>
    public long TotalClicks { get; set; }

    /// <summary>Prestige-Währung: Paragraphen. Jeder gibt dauerhaft +5 % Produktion.</summary>
    public int Paragraphen { get; set; }

    /// <summary>Anzahl durchgeführter Verwaltungsreformen (Prestige-Resets).</summary>
    public int TotalReformen { get; set; }

    /// <summary>Stufe des Klick-Upgrades ("Stempelkissen"). Klickkraft = 2^Stufe.</summary>
    public int ClickUpgradeLevel { get; set; }

    /// <summary>Zustand aller Generatoren, indiziert über die Generator-Id.</summary>
    public Dictionary<string, GeneratorState> Generators { get; set; } = new();

    /// <summary>Ids der freigeschalteten Achievements.</summary>
    public HashSet<string> UnlockedAchievements { get; set; } = new();

    /// <summary>Stufen der Fortbildungen (Id → Level). Verfallen bei Reformen.</summary>
    public Dictionary<string, int> ResearchLevels { get; set; } = new();

    /// <summary>Veraltet (v1.1.0): einstufige Forschung. Wird beim Laden nach <see cref="ResearchLevels"/> migriert.</summary>
    public HashSet<string> ResearchedIds { get; set; } = new();

    public int GetResearchLevel(string id) => ResearchLevels.GetValueOrDefault(id);

    /// <summary>Zeitpunkt des letzten Speicherns (UTC) — Basis für Offline-Fortschritt.</summary>
    public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;

    public GeneratorState GetGenerator(string id)
    {
        if (!Generators.TryGetValue(id, out var state))
        {
            state = new GeneratorState();
            Generators[id] = state;
        }
        return state;
    }
}

/// <summary>Persistierter Zustand eines einzelnen Generators.</summary>
public sealed class GeneratorState
{
    public int Owned { get; set; }
    public bool AutoBuyerOwned { get; set; }
    public bool AutoBuyerEnabled { get; set; }
}
