using Amtsschimmel.Models;
using Amtsschimmel.Services;
using Xunit;

namespace Amtsschimmel.Tests;

public sealed class TransferAndUpdateTests
{
    // ---------- Spielstand-Export/Import ----------

    [Fact]
    public void ExportImport_Roundtrip_ErhaeltAlleWerte()
    {
        var save = new SaveGameService();
        var state = new GameState
        {
            Stempel = 12_345.6,
            Paragraphen = 42,
            TotalReformen = 3,
            GoldenFormsClicked = 7,
        };
        state.ResearchLevels["zehnfinger"] = 4;
        state.GetGenerator("praktikant").Owned = 99;

        var imported = save.TryImport(save.Export(state));

        Assert.NotNull(imported);
        Assert.Equal(12_345.6, imported.Stempel);
        Assert.Equal(42, imported.Paragraphen);
        Assert.Equal(3, imported.TotalReformen);
        Assert.Equal(7, imported.GoldenFormsClicked);
        Assert.Equal(4, imported.GetResearchLevel("zehnfinger"));
        Assert.Equal(99, imported.GetGenerator("praktikant").Owned);
    }

    [Theory]
    [InlineData("")]
    [InlineData("kein export")]
    [InlineData("AMT1:das-ist-kein-base64!!!")]
    [InlineData("XYZ9:aGFsbG8=")]
    public void Import_LehntUngueltigeEingabenAb(string input)
    {
        Assert.Null(new SaveGameService().TryImport(input));
    }

    // ---------- Versions-Parsing für den Update-Check ----------

    [Theory]
    [InlineData("v1.7.0", "1.7.0")]
    [InlineData("1.8.0", "1.8.0")]
    [InlineData("1.8.0+abc123", "1.8.0")]
    [InlineData("v2.0.0-rc1", "2.0.0")]
    [InlineData("1.2", "1.2.0")]
    public void ParseVersion_NormalisiertTagsUndMetadaten(string input, string expected)
    {
        Assert.Equal(Version.Parse(expected), UpdateCheckService.ParseVersion(input));
    }

    [Fact]
    public void ParseVersion_NullBeiUnsinn()
    {
        Assert.Null(UpdateCheckService.ParseVersion("kaputt"));
    }

    // ---------- Einstellungen ----------

    [Fact]
    public void Settings_RoundtripUeberDatei()
    {
        var dir = Path.Combine(Path.GetTempPath(), "amt-settings-" + Guid.NewGuid());
        var first = new SettingsService(dir);
        first.Current.AutosaveIntervalSeconds = 120;
        first.Current.TickerEnabled = false;
        first.Save();

        var second = new SettingsService(dir);
        Assert.Equal(120, second.Current.AutosaveIntervalSeconds);
        Assert.False(second.Current.TickerEnabled);
    }
}
