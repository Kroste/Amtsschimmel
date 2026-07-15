using System.Globalization;

namespace Amtsschimmel.Services;

/// <summary>Formatiert große Zahlen mit deutschen Suffixen (Tsd., Mio., Mrd., …).</summary>
public static class NumberFormatter
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-DE");

    private static readonly (double Threshold, string Suffix)[] Suffixes =
    [
        (1e18, "Trill."),
        (1e15, "Brd."),
        (1e12, "Bio."),
        (1e9,  "Mrd."),
        (1e6,  "Mio."),
        (1e3,  "Tsd."),
    ];

    public static string Format(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "∞";
        }
        var abs = Math.Abs(value);
        if (abs >= 1e21)
        {
            return value.ToString("0.00e0", De);
        }
        foreach (var (threshold, suffix) in Suffixes)
        {
            if (abs >= threshold)
            {
                return (value / threshold).ToString("0.##", De) + " " + suffix;
            }
        }
        return abs < 100
            ? value.ToString("0.#", De)
            : value.ToString("N0", De);
    }
}
