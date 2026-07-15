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
    public void BuyResearch_ZiehtKostenAbUndSchaltetFrei()
    {
        var engine = EngineWithStempel(1_000);
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.Equal(500, engine.State.Stempel);
        Assert.True(engine.IsResearched(Get("zehnfinger")));
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
    public void BuyResearch_ScheitertBeiDoppelkauf()
    {
        var engine = EngineWithStempel(10_000);
        Assert.True(engine.BuyResearch(Get("zehnfinger")));
        Assert.False(engine.BuyResearch(Get("zehnfinger")));
    }

    [Fact]
    public void ClickMultiplier_WirktAufKlickkraft()
    {
        var engine = EngineWithStempel(500);
        var basis = engine.ClickPower;
        engine.BuyResearch(Get("zehnfinger")); // ×2
        Assert.Equal(basis * 2, engine.ClickPower, precision: 10);
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
    public void AllGeneratorsMultiplier_WirktUeberall()
    {
        var engine = EngineWithStempel(10_000);
        engine.BuyResearch(Get("ordner")); // ×1,25 alle
        Assert.Equal(1.25, engine.ResearchMultiplierFor("ki_cloud"), precision: 10);
    }

    [Fact]
    public void CostReduction_ReduziertGeneratorkostenMultiplikativ()
    {
        var engine = EngineWithStempel(1e9);
        engine.State.ResearchedIds.Add("sammelbestellung"); // −5 %
        engine.State.ResearchedIds.Add("beschaffung");      // −10 %
        Assert.Equal(0.95 * 0.90, engine.CostFactor, precision: 10);

        var praktikant = GameDefinitions.Generators[0];
        Assert.Equal(15 * 0.95 * 0.90, engine.NextCost(praktikant), precision: 10);
    }

    [Fact]
    public void OfflineForschung_ErhoehtEffizienzUndCap()
    {
        var engine = new GameEngine();
        Assert.Equal(0.5, engine.OfflineEfficiency);
        Assert.Equal(TimeSpan.FromHours(8), engine.OfflineCap);

        engine.State.ResearchedIds.Add("gleitzeit");  // 75 %
        engine.State.ResearchedIds.Add("homeoffice"); // 24 h
        Assert.Equal(0.75, engine.OfflineEfficiency);
        Assert.Equal(TimeSpan.FromHours(24), engine.OfflineCap);
    }

    [Fact]
    public void ParagraphBonus_ErhoehtReformErtrag()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 16e6; // √16 = 4 Basis-Paragraphen
        Assert.Equal(4, engine.PendingParagraphen);

        engine.State.ResearchedIds.Add("reformkommission"); // +25 %
        Assert.Equal(5, engine.PendingParagraphen); // 4 × 1,25 = 5
    }

    [Fact]
    public void Prestige_SetztForschungZurueck()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 4e6;
        engine.State.ResearchedIds.Add("zehnfinger");

        engine.Prestige();

        Assert.Empty(engine.State.ResearchedIds);
    }
}
