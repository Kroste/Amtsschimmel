using Amtsschimmel.Models;

namespace Amtsschimmel.Services;

/// <summary>
/// Liefert satirische Amtsblatt-Meldungen für den Ticker — teils generisch,
/// teils abhängig vom Spielstand (Postillon-Stil, siehe Magnat/Havelstädter Bote).
/// </summary>
public sealed class AmtsblattService
{
    private readonly Random _rng = new();
    private string _lastHeadline = "";

    private static readonly string[] Generic =
    [
        "Ausschreibung für neue Ausschreibungsrichtlinie ausgeschrieben.",
        "Flurfunk meldet: Kaffeemaschine im 3. OG hat wieder Bohnen.",
        "Formular 27b/6 jetzt auch in Ausfertigung 27b/6a erhältlich.",
        "Praktikant findet Aktenordner von 1987 — Inhalt: ein weiterer Aktenordner.",
        "Umlaufmappe seit 14 Tagen im Umlauf. Verbleib ungeklärt.",
        "Neue Dienstanweisung regelt die Anwendung alter Dienstanweisungen.",
        "Poststelle vermeldet Rekord: Brief nach nur drei Tagen intern zugestellt.",
        "Arbeitskreis 'Weniger Arbeitskreise' konstituiert sich am Donnerstag.",
        "Der Aufzug im Westflügel wird gewartet. Seit 2019.",
        "Toner ist alle. Ein Antrag auf neuen Toner liegt zur Unterschrift bereit.",
        "Brandschutzübung verschoben — das Formular zur Verschiebung ist unauffindbar.",
        "Kantine führt Currywurst-Mittwoch ein. Der Personalrat prüft.",
        "Parkplatzvergabe erneut vertagt: Sitzungssaal war belegt.",
        "Wichtige Durchsage: Die Durchsageanlage funktioniert wieder.",
        "Stempelkissen-Inventur ergibt: mehr Kissen als Stempel.",
        "IT meldet: Das Problem sitzt gelegentlich vor dem Bildschirm.",
        "Faxgerät in Zimmer 214 gilt weiterhin als systemrelevant.",
        "Zuständigkeitsprüfung ergibt: zuständig ist die Zuständigkeitsprüfstelle.",
    ];

    private static readonly (Func<GameState, bool> Condition, Func<GameState, string> Text)[] Conditional =
    [
        (s => s.TotalReformen >= 1,
            _ => "Nach der Reform ist vor der Reform, bestätigt das Reformreferat."),
        (s => s.TotalReformen >= 3,
            s => $"Bürger bemerken von Reform Nr. {s.TotalReformen} erneut nichts. Das Amt wertet das als Erfolg."),
        (s => s.Paragraphen >= 50,
            _ => "Der Paragraphendschungel wurde offiziell zum Naherholungsgebiet erklärt."),
        (s => s.GetGenerator("praktikant").Owned >= 100,
            _ => "Praktikanten fordern eigene Kaffeeküche. Der Antrag ist in Prüfung."),
        (s => s.GetGenerator("ki_cloud").Owned >= 1,
            _ => "KI-Cloud stempelt ein Formular ab, das sie selbst erstellt hat. Singularität vertagt."),
        (s => s.Generators.Values.Any(g => g.AutoBuyerOwned),
            _ => "Beschaffungsautomat kauft versehentlich einen zweiten Beschaffungsautomaten."),
        (s => s.TotalClicks >= 10_000,
            _ => "Betriebsarzt lobt die Stempelhand der Amtsleitung: 'Beeindruckende Muskulatur.'"),
        (s => s.GoldenFormsClicked >= 1,
            _ => "Goldenes Formular gesichtet — Rechnungsprüfung vermutet Druckfehler."),
    ];

    /// <summary>Nächste Schlagzeile: ~40 % zustandsbezogen (falls verfügbar), sonst generisch.</summary>
    public string NextHeadline(GameState state)
    {
        var eligible = Conditional.Where(c => c.Condition(state)).ToArray();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var headline = eligible.Length > 0 && _rng.Next(100) < 40
                ? eligible[_rng.Next(eligible.Length)].Text(state)
                : Generic[_rng.Next(Generic.Length)];
            if (headline != _lastHeadline)
            {
                _lastHeadline = headline;
                return headline;
            }
        }
        return _lastHeadline;
    }
}
