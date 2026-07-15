using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class JuiceFeatureTests
{
    private static GeneratorDefinition Praktikant => GameDefinitions.Generators[0];

    // ---------- Meilensteine ----------

    [Fact]
    public void Meilenstein_VerdoppeltJeSchwelle()
    {
        var engine = new GameEngine();
        var gen = engine.State.GetGenerator(Praktikant.Id);

        gen.Owned = 9;
        Assert.Equal(1, engine.MilestoneMultiplierFor(Praktikant.Id));
        gen.Owned = 10;
        Assert.Equal(2, engine.MilestoneMultiplierFor(Praktikant.Id));
        gen.Owned = 50;  // 10, 25, 50 erreicht
        Assert.Equal(8, engine.MilestoneMultiplierFor(Praktikant.Id));
        gen.Owned = 200; // alle 6 Schwellen
        Assert.Equal(64, engine.MilestoneMultiplierFor(Praktikant.Id));
    }

    [Fact]
    public void Meilenstein_EventFeuertBeimUeberschreiten_AuchBeiBulkKauf()
    {
        var engine = new GameEngine();
        engine.State.Stempel = 1e9;
        engine.State.GetGenerator(Praktikant.Id).Owned = 8;

        var reached = new List<int>();
        engine.MilestoneReached += (_, threshold) => reached.Add(threshold);

        engine.BuyGenerator(Praktikant, 20); // 8 → 28: überschreitet 10 UND 25

        Assert.Equal([10, 25], reached);
    }

    [Fact]
    public void NextMilestone_LiefertNaechsteSchwelleOderNull()
    {
        var engine = new GameEngine();
        var gen = engine.State.GetGenerator(Praktikant.Id);
        Assert.Equal(10, engine.NextMilestoneFor(Praktikant.Id));
        gen.Owned = 60;
        Assert.Equal(100, engine.NextMilestoneFor(Praktikant.Id));
        gen.Owned = 200;
        Assert.Null(engine.NextMilestoneFor(Praktikant.Id));
    }

    // ---------- Buff (Goldene Formulare) ----------

    [Fact]
    public void Buff_MultipliziertProduktionUndLaeuftAb()
    {
        var engine = new GameEngine();
        engine.State.GetGenerator(Praktikant.Id).Owned = 1; // 0,1/s
        var basis = engine.TotalProductionPerSecond();

        engine.ActivateProductionBuff(7, TimeSpan.FromSeconds(30));
        Assert.True(engine.IsBuffActive);
        Assert.Equal(basis * 7, engine.TotalProductionPerSecond(), precision: 10);

        engine.ActivateProductionBuff(7, TimeSpan.FromSeconds(-1)); // sofort abgelaufen
        Assert.False(engine.IsBuffActive);
        Assert.Equal(basis, engine.TotalProductionPerSecond(), precision: 10);
    }

    [Fact]
    public void GoldenesFormular_ZaehltUndBelohnt()
    {
        var engine = new GameEngine();
        var beforeStempel = engine.State.Stempel;

        var text = engine.GrantGoldenFormReward(new Random(42));

        Assert.Equal(1, engine.State.GoldenFormsClicked);
        Assert.False(string.IsNullOrWhiteSpace(text));
        // Entweder Sofort-Stempel ODER aktiver Buff — eins von beidem muss greifen.
        Assert.True(engine.State.Stempel > beforeStempel || engine.IsBuffActive);
    }

    // ---------- Amtsblatt ----------

    [Fact]
    public void Amtsblatt_LiefertImmerEineMeldung()
    {
        var service = new AmtsblattService();
        var state = new GameState();
        for (var i = 0; i < 50; i++)
        {
            Assert.False(string.IsNullOrWhiteSpace(service.NextHeadline(state)));
        }
    }

    [Fact]
    public void Amtsblatt_WiederholtSichNichtDirekt()
    {
        var service = new AmtsblattService();
        var state = new GameState();
        var last = service.NextHeadline(state);
        for (var i = 0; i < 30; i++)
        {
            var next = service.NextHeadline(state);
            Assert.NotEqual(last, next);
            last = next;
        }
    }

    // ---------- Statistik ----------

    [Fact]
    public void Tick_SammeltSpielzeitUndEinkommensrekord()
    {
        var engine = new GameEngine();
        engine.State.GetGenerator(Praktikant.Id).Owned = 1; // 0,1/s

        engine.Tick(2);
        engine.Tick(3);

        Assert.Equal(5, engine.State.TotalPlaySeconds, precision: 6);
        // Tick 1 schaltet das Achievement "Einstellungszusage" frei (+1 %),
        // Tick 2 misst daher 0,1 × 1,01 = 0,101 als Rekord.
        Assert.Equal(0.101, engine.State.HighestIncomePerSec, precision: 6);
    }
}
