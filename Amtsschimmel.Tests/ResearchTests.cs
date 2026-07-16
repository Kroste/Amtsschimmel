using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class ResearchTests
{
    private static ResearchDefinition Get(string id) =>
        ResearchDefinitions.All.First(r => r.Id == id);

    private static GameEngine EngineWithStempel(double stempel)
    {
        var engine = new GameEngine();
        engine.State.Stempel = stempel;
        return engine;
    }

    [Fact]
    public void AlleVoraussetzungen_ZeigenAufExistierendeForschungen()
    {
        // Schützt vor Tippfehlern im Baum: jede Prerequisite-Id muss existieren.
        var ids = ResearchDefinitions.All.Select(r => r.Id).ToHashSet();
        foreach (var research in ResearchDefinitions.All)
        {
            foreach (var prereq in research.Prerequisites ?? [])
            {
                Assert.Contains(prereq, ids);
            }
        }
    }

    [Fact]
    public void AlleGeneratorTargets_ZeigenAufExistierendeGeneratoren()
    {
        var ids = GameDefinitions.Generators.Select(g => g.Id).ToHashSet();
        foreach (var research in ResearchDefinitions.All)
        {
            foreach (var target in research.TargetGeneratorIds ?? [])
            {
                Assert.Contains(target, ids);
            }
        }
    }

    [Fact]
    public void BuyResearch_ZiehtKostenAbUndErhoehtStufe()
    {
        var engine = EngineWithStempel(1_000);
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.Equal(500, engine.State.Stempel);
        Assert.Equal(1, engine.ResearchLevel(Get("zehnfinger")));
    }

    [Fact]
    public void BuyResearch_ScheitertOhneVoraussetzung()
    {
        // "stempelhalter" benötigt "zehnfinger".
        var engine = EngineWithStempel(1e9);
        Assert.False(engine.BuyResearch(Get("stempelhalter")));
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.True(engine.BuyResearch(Get("stempelhalter")));
    }

    [Fact]
    public void Mehrfachforschung_KostenWachsenProStufe()
    {
        // zehnfinger: Basis 500, Wachstum ×8 → Stufe 2 kostet 4.000.
        var engine = EngineWithStempel(4_500);
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.Equal(4_000, engine.NextResearchCost(Get("zehnfinger")));
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.Equal(2, engine.ResearchLevel(Get("zehnfinger")));
        Assert.Equal(0, engine.State.Stempel);
    }

    [Fact]
    public void Mehrfachforschung_EffekteStapelnMultiplikativ()
    {
        var engine = new GameEngine();
        var basis = engine.ClickPower;
        engine.State.ResearchLevels["zehnfinger"] = 3; // ×2 je Stufe → ×8
        Assert.Equal(basis * 8, engine.ClickPower, precision: 10);
    }

    [Fact]
    public void Mehrfachforschung_StopptBeiMaximalstufe()
    {
        var engine = EngineWithStempel(double.MaxValue / 2);
        var zehnfinger = Get("zehnfinger"); // MaxLevel 5
        for (var i = 0; i < 5; i++)
        {
            Assert.True(engine.BuyResearch(zehnfinger));
        }
        Assert.True(engine.IsMaxed(zehnfinger));
        Assert.False(engine.BuyResearch(zehnfinger));
        Assert.Equal(5, engine.ResearchLevel(zehnfinger));
    }

    [Fact]
    public void EndlosForschung_HatKeineMaximalstufe()
    {
        var engine = EngineWithStempel(double.MaxValue / 2);
        var abbau = Get("buerokratieabbau");
        engine.State.ResearchLevels["lean_admin"] = 1; // Voraussetzung
        engine.State.TotalReformen = 3;                // seit v1.3.0 hinter Reform 3 gesperrt

        for (var i = 0; i < 20; i++)
        {
            Assert.True(engine.BuyResearch(abbau));
        }
        Assert.False(engine.IsMaxed(abbau));
        Assert.Equal(20, engine.ResearchLevel(abbau));
        // ×1,1 je Stufe → ×1,1^20; die Voraussetzung lean_admin steuert selbst ×2 bei.
        Assert.Equal(Math.Pow(1.1, 20) * 2, engine.ResearchMultiplierFor("praktikant"), precision: 8);
    }

    [Fact]
    public void GeneratorMultiplier_WirktNurAufZielGeneratoren()
    {
        var engine = EngineWithStempel(2_500);
        engine.BuyResearch(Get("kaffeekueche")); // ×2 für Praktikant + Sachbearbeiter
        Assert.Equal(2, engine.ResearchMultiplierFor("praktikant"), precision: 10);
        Assert.Equal(1, engine.ResearchMultiplierFor("teamleiter"), precision: 10);
    }

    [Fact]
    public void Matrixorg_BoosetFachbereichUndDezernat()
    {
        // Schließt die Mittelbau-Lücke: fachbereich/dezernat kriegen den ×2-Boost, andere nicht.
        var engine = new GameEngine();
        engine.State.ResearchLevels["matrixorg"] = 3; // 2³ = 8
        Assert.Equal(8, engine.ResearchMultiplierFor("fachbereich"), precision: 10);
        Assert.Equal(8, engine.ResearchMultiplierFor("dezernat"), precision: 10);
        Assert.Equal(1, engine.ResearchMultiplierFor("amtsleiter"), precision: 10);
        Assert.Equal(1, engine.ResearchMultiplierFor("rathaus"), precision: 10);
    }

    [Fact]
    public void Foederalismus_BoosetRathausUndLandesbehoerde()
    {
        var engine = new GameEngine();
        engine.State.ResearchLevels["foederalismus"] = 3;
        Assert.Equal(8, engine.ResearchMultiplierFor("rathaus"), precision: 10);
        Assert.Equal(8, engine.ResearchMultiplierFor("landesbehoerde"), precision: 10);
        Assert.Equal(1, engine.ResearchMultiplierFor("dezernat"), precision: 10);
        Assert.Equal(1, engine.ResearchMultiplierFor("ministerium"), precision: 10);
    }

    [Fact]
    public void CostReduction_StapeltJeStufeUndUeberKnoten()
    {
        var engine = new GameEngine();
        engine.State.ResearchLevels["sammelbestellung"] = 3; // 0,95³
        engine.State.ResearchLevels["beschaffung"] = 2;      // 0,90²
        Assert.Equal(Math.Pow(0.95, 3) * Math.Pow(0.90, 2), engine.CostFactor, precision: 10);
    }

    [Fact]
    public void OfflineForschung_ErhoehtEffizienzUndCap()
    {
        var engine = new GameEngine();
        Assert.Equal(0.5, engine.OfflineEfficiency);
        Assert.Equal(TimeSpan.FromHours(8), engine.OfflineCap);

        engine.State.ResearchLevels["gleitzeit"] = 1;  // 75 %
        engine.State.ResearchLevels["homeoffice"] = 1; // 24 h
        Assert.Equal(0.75, engine.OfflineEfficiency);
        Assert.Equal(TimeSpan.FromHours(24), engine.OfflineCap);
    }

    [Fact]
    public void ParagraphBonus_StapeltJeStufe()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 16e6; // √16 = 4 Basis-Paragraphen
        Assert.Equal(4, engine.PendingParagraphen);

        engine.State.ResearchLevels["reformkommission"] = 2; // (1,25)² = 1,5625
        Assert.Equal(6, engine.PendingParagraphen); // ⌊4 × 1,5625⌋ = 6
    }

    [Fact]
    public void Prestige_SetztForschungZurueck()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 4e6;
        engine.State.ResearchLevels["zehnfinger"] = 3;

        engine.Prestige();

        Assert.Empty(engine.State.ResearchLevels);
    }

    [Fact]
    public void LoadState_MigriertAlteEinstufigeForschung()
    {
        // Save aus v1.1.0: ResearchedIds statt ResearchLevels.
        var oldState = new GameState();
        oldState.ResearchedIds.Add("zehnfinger");
        oldState.ResearchedIds.Add("ordner");

        var engine = new GameEngine();
        engine.LoadState(oldState);

        Assert.Equal(1, engine.State.GetResearchLevel("zehnfinger"));
        Assert.Equal(1, engine.State.GetResearchLevel("ordner"));
        Assert.Empty(engine.State.ResearchedIds);
    }
}
