using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class VictoryTests
{
    private static GameEngine ReadyToWinEngine()
    {
        var engine = new GameEngine();
        engine.State.Stempel = GameEngine.VictoryCost;
        engine.State.TotalReformen = GameEngine.VictoryMinReformen;
        foreach (var research in ResearchDefinitions.All)
        {
            engine.State.ResearchLevels[research.Id] = 1;
        }
        return engine;
    }

    [Fact]
    public void CanWin_NurWennAlleBedingungenErfuellt()
    {
        var engine = ReadyToWinEngine();
        Assert.True(engine.CanWin);

        // Achtung double-Präzision: 1e18 − 1 == 1e18 (ULP ≈ 128) — daher halbe Kosten testen.
        engine.State.Stempel = GameEngine.VictoryCost / 2;
        Assert.False(engine.CanWin); // zu wenig Stempel

        engine.State.Stempel = GameEngine.VictoryCost;
        engine.State.TotalReformen = GameEngine.VictoryMinReformen - 1;
        Assert.False(engine.CanWin); // zu wenige Reformen

        engine.State.TotalReformen = GameEngine.VictoryMinReformen;
        engine.State.ResearchLevels.Remove("zehnfinger");
        Assert.False(engine.CanWin); // Forschung unvollständig
    }

    [Fact]
    public void Win_ZiehtKostenAbUndMarkiertSieg()
    {
        var engine = ReadyToWinEngine();
        Assert.True(engine.Win());

        Assert.Equal(0, engine.State.Stempel);
        Assert.True(engine.State.HasWon);
        Assert.NotNull(engine.State.WonAtUtc);
        Assert.NotNull(engine.State.WonAfterPlaySeconds);
    }

    [Fact]
    public void Win_NurEinmalMoeglich()
    {
        var engine = ReadyToWinEngine();
        Assert.True(engine.Win());
        engine.State.Stempel = GameEngine.VictoryCost;
        Assert.False(engine.CanWin);
        Assert.False(engine.Win());
    }

    [Fact]
    public void Sieg_SchaltetAchievementFrei()
    {
        var engine = ReadyToWinEngine();
        engine.Win();
        engine.Tick(0.1); // Achievement-Prüfung läuft im Tick
        Assert.Contains("victory", engine.State.UnlockedAchievements);
    }

    [Fact]
    public void Prestige_LaesstSiegUnberuehrt()
    {
        // Endlosmodus: Auch nach dem Sieg darf reformiert werden, der Sieg bleibt.
        var engine = ReadyToWinEngine();
        engine.Win();
        engine.State.TotalEarnedThisRun = 1e60;
        engine.Prestige();
        Assert.True(engine.State.HasWon);
    }
}
