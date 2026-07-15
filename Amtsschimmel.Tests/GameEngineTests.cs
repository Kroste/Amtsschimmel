using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class GameEngineTests
{
    private static GeneratorDefinition Praktikant => GameDefinitions.Generators[0];

    [Fact]
    public void Click_ErhoehtStempelUmKlickkraft()
    {
        var engine = new GameEngine();
        engine.Click();
        Assert.Equal(1, engine.State.Stempel);
        Assert.Equal(1, engine.State.TotalClicks);
    }

    [Fact]
    public void BuyGenerator_ZiehtKostenAbUndErhoehtBestand()
    {
        var engine = new GameEngine();
        engine.State.Stempel = 100;
        Assert.True(engine.BuyGenerator(Praktikant));
        Assert.Equal(1, engine.State.GetGenerator(Praktikant.Id).Owned);
        Assert.Equal(85, engine.State.Stempel); // 100 − 15 Basiskosten
    }

    [Fact]
    public void BuyGenerator_SchlaegtFehlOhneGeld()
    {
        var engine = new GameEngine();
        engine.State.Stempel = 10;
        Assert.False(engine.BuyGenerator(Praktikant));
        Assert.Equal(10, engine.State.Stempel);
    }

    [Fact]
    public void CostFor_WaechstExponentiell()
    {
        // 2. Einheit = Basiskosten × 1,15
        Assert.Equal(15 * 1.15, Praktikant.CostFor(1), precision: 10);
    }

    [Fact]
    public void CostForBulk_EntsprichtSummeDerEinzelkosten()
    {
        var einzeln = Enumerable.Range(0, 10).Sum(i => Praktikant.CostFor(i));
        Assert.Equal(einzeln, Praktikant.CostForBulk(0, 10), precision: 6);
    }

    [Fact]
    public void Tick_ProduziertStempelProportionalZurZeit()
    {
        var engine = new GameEngine();
        engine.State.GetGenerator(Praktikant.Id).Owned = 10; // 10 × 0,1/s = 1/s
        engine.Tick(deltaSeconds: 5);
        Assert.Equal(5, engine.State.Stempel, precision: 6);
    }

    [Fact]
    public void Prestige_NichtMoeglichUnterSchwelle()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = GameEngine.PrestigeThreshold - 1;
        Assert.False(engine.CanPrestige);
        Assert.Equal(0, engine.Prestige());
    }

    [Fact]
    public void Prestige_GewaehrtParagraphenUndResettet()
    {
        var engine = new GameEngine();
        engine.State.TotalEarnedThisRun = 4e6; // √4 = 2 Paragraphen
        engine.State.Stempel = 12345;
        engine.State.ClickUpgradeLevel = 3;
        var gen = engine.State.GetGenerator(Praktikant.Id);
        gen.Owned = 50;
        gen.AutoBuyerOwned = true;

        var earned = engine.Prestige();

        Assert.Equal(2, earned);
        Assert.Equal(2, engine.State.Paragraphen);
        Assert.Equal(0, engine.State.Stempel);
        Assert.Equal(0, engine.State.ClickUpgradeLevel);
        Assert.Equal(0, gen.Owned);
        Assert.True(gen.AutoBuyerOwned); // Auto-Buyer überleben die Reform
    }

    [Fact]
    public void GlobalMultiplier_BeruecksichtigtParagraphenUndAchievements()
    {
        var engine = new GameEngine();
        engine.State.Paragraphen = 10;                      // ×1,5
        engine.State.UnlockedAchievements.Add("earn_1k");   // ×1,01
        Assert.Equal(1.5 * 1.01, engine.GlobalMultiplier, precision: 10);
    }

    [Fact]
    public void AutoBuyer_KauftAutomatischWennAktivUndBezahlbar()
    {
        var engine = new GameEngine();
        var gen = engine.State.GetGenerator(Praktikant.Id);
        gen.AutoBuyerOwned = true;
        gen.AutoBuyerEnabled = true;
        engine.State.Stempel = 20;

        engine.Tick(0.1);

        Assert.Equal(1, gen.Owned);
        Assert.Equal(5, engine.State.Stempel, precision: 6);
    }

    [Fact]
    public void AutoBuyer_KauftNichtWennDeaktiviert()
    {
        var engine = new GameEngine();
        var gen = engine.State.GetGenerator(Praktikant.Id);
        gen.AutoBuyerOwned = true;
        gen.AutoBuyerEnabled = false;
        engine.State.Stempel = 20;

        engine.Tick(0.1);

        Assert.Equal(0, gen.Owned);
    }

    [Fact]
    public void OfflineProgress_GekapptUndMitEffizienzfaktor()
    {
        var engine = new GameEngine();
        engine.State.GetGenerator(Praktikant.Id).Owned = 10; // 1/s
        engine.State.LastSavedUtc = DateTime.UtcNow - TimeSpan.FromHours(24);

        var result = engine.ApplyOfflineProgress();

        Assert.NotNull(result);
        var (duration, earned) = result.Value;
        Assert.Equal(GameEngine.OfflineCap, duration); // 24 h → gekappt auf 8 h
        // 8 h × 3600 s × 1/s × 0,5 Effizienz = 14.400
        Assert.Equal(14_400, earned, precision: 3);
    }

    [Fact]
    public void OfflineProgress_NullBeiKurzerAbwesenheit()
    {
        var engine = new GameEngine();
        engine.State.GetGenerator(Praktikant.Id).Owned = 10;
        engine.State.LastSavedUtc = DateTime.UtcNow - TimeSpan.FromSeconds(30);
        Assert.Null(engine.ApplyOfflineProgress());
    }

    [Fact]
    public void Achievements_WerdenBeimTickFreigeschaltet()
    {
        var engine = new GameEngine();
        AchievementDefinition? unlocked = null;
        engine.AchievementUnlocked += a => unlocked = a;

        engine.Click();     // "Dienstbeginn"-Bedingung erfüllt
        engine.Tick(0.1);   // Prüfung läuft im Tick

        Assert.Contains("first_click", engine.State.UnlockedAchievements);
        Assert.NotNull(unlocked);
    }

    [Fact]
    public void NumberFormatter_DeutscheSuffixe()
    {
        Assert.Equal("1,5 Mio.", NumberFormatter.Format(1_500_000));
        Assert.Equal("2 Mrd.", NumberFormatter.Format(2e9));
        Assert.Equal("42", NumberFormatter.Format(42));
    }
}
