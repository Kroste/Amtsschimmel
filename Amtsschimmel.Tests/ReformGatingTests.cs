using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class ReformGatingTests
{
    private static GeneratorDefinition Gen(string id) =>
        GameDefinitions.Generators.First(g => g.Id == id);

    private static ResearchDefinition Res(string id) =>
        ResearchDefinitions.All.First(r => r.Id == id);

    // ---------- Steigende Reform-Schwelle ----------

    [Fact]
    public void ReformSchwelle_VerzehnfachtSichProReform()
    {
        var engine = new GameEngine();
        Assert.Equal(1e6, engine.CurrentPrestigeThreshold);
        engine.State.TotalReformen = 1;
        Assert.Equal(1e7, engine.CurrentPrestigeThreshold);
        engine.State.TotalReformen = 3;
        Assert.Equal(1e9, engine.CurrentPrestigeThreshold);
    }

    [Fact]
    public void ZweiteReform_BrauchtNeueSchwelle()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 4e6;
        engine.Prestige(); // Reform 1 → Schwelle jetzt 1e7

        engine.State.TotalEarnedThisRun = 4e6; // reichte früher, jetzt nicht mehr
        Assert.False(engine.CanPrestige);

        engine.State.TotalEarnedThisRun = 4e7; // √(4e7/1e7) = 2
        Assert.Equal(2, engine.PendingParagraphen);
    }

    // ---------- Reform-Gating ----------

    [Fact]
    public void GesperrterGenerator_NichtKaufbarVorReform()
    {
        var engine = new GameEngine();
        engine.State.Stempel = 1e15;
        Assert.False(engine.BuyGenerator(Gen("ministerium"))); // MinReformen 1
        Assert.Equal(1e15, engine.State.Stempel);

        engine.State.TotalReformen = 1;
        Assert.True(engine.BuyGenerator(Gen("ministerium")));
    }

    [Fact]
    public void GesperrteForschung_NichtKaufbarVorReform()
    {
        var engine = new GameEngine();
        engine.State.Stempel = 1e15;
        engine.State.ResearchLevels["digitale_akte"] = 1; // Prereq von lean_admin erfüllt

        Assert.False(engine.BuyResearch(Res("lean_admin"))); // MinReformen 1
        engine.State.TotalReformen = 1;
        Assert.True(engine.BuyResearch(Res("lean_admin")));
    }

    [Fact]
    public void AlleMinReformen_SindErreichbarKleinGehalten()
    {
        // Schutz vor Content, der nie freischaltet: Gating maximal Reform 5.
        foreach (var g in GameDefinitions.Generators)
        {
            Assert.InRange(g.MinReformen, 0, 5);
        }
        foreach (var r in ResearchDefinitions.All)
        {
            Assert.InRange(r.MinReformen, 0, 5);
        }
    }

    // ---------- Auto-Stempeln ----------

    [Fact]
    public void Stempelautomat_ProduziertKlickkraftProSekunde()
    {
        var engine = new GameEngine();
        engine.State.ResearchLevels["stempelautomat"] = 3; // 3 Klicks/s
        Assert.Equal(3, engine.AutoClicksPerSecond);

        // Klickkraft = 1 (Basis) → 3 Stempel/s; Tick über 2 s → 6 Stempel.
        engine.Tick(2);
        Assert.Equal(6, engine.State.Stempel, precision: 6);
    }

    [Fact]
    public void Stempelautomat_ZaehltNichtAlsManuellerKlick()
    {
        // Wichtig für Klick-Achievements: nur echte Klicks zählen.
        var engine = new GameEngine();
        engine.State.ResearchLevels["stempelautomat"] = 5;
        engine.Tick(10);
        Assert.Equal(0, engine.State.TotalClicks);
    }

    [Fact]
    public void Stempelautomat_SkaliertMitKlickkraft()
    {
        var engine = new GameEngine();
        engine.State.ResearchLevels["stempelautomat"] = 2; // 2 Klicks/s
        engine.State.ClickUpgradeLevel = 3;                // Klickkraft 8
        Assert.Equal(engine.ClickPower * 2, engine.EffectiveIncomePerSecond(), precision: 6);
    }

    [Fact]
    public void Stempelautomat_WirktAuchOffline()
    {
        var engine = new GameEngine();
        engine.State.ResearchLevels["stempelautomat"] = 1; // 1 Klick/s, Klickkraft 1
        engine.State.LastSavedUtc = DateTime.UtcNow - TimeSpan.FromHours(2);

        var result = engine.ApplyOfflineProgress();

        Assert.NotNull(result);
        // 2 h × 3600 s × 1/s × 0,5 Effizienz = 3.600
        Assert.Equal(3_600, result.Value.Earned, precision: 0);
    }
}
