using System.Globalization;
using Amtsschimmel.Models;
using Amtsschimmel.Services;

namespace Amtsschimmel.ViewModels;

/// <summary>Statistiken für den Siegesbildschirm.</summary>
public sealed class VictoryViewModel
{
    public string PlayTimeText { get; }
    public string ReformenText { get; }
    public string ParagraphenText { get; }
    public string AllTimeText { get; }
    public string ClicksText { get; }

    public VictoryViewModel(GameState state)
    {
        var t = TimeSpan.FromSeconds(state.WonAfterPlaySeconds ?? state.TotalPlaySeconds);
        PlayTimeText = t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes} min" : $"{t.Minutes} min";
        ReformenText = state.TotalReformen.ToString();
        ParagraphenText = state.Paragraphen.ToString("N0", CultureInfo.GetCultureInfo("de-DE")) + " §";
        AllTimeText = NumberFormatter.Format(state.TotalEarnedAllTime);
        ClicksText = state.TotalClicks.ToString("N0", CultureInfo.GetCultureInfo("de-DE"));
    }
}
