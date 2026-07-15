using Avalonia.Data.Converters;

namespace Amtsschimmel.ViewModels;

/// <summary>Statische Value-Converter für XAML-Bindings.</summary>
public static class Converters
{
    /// <summary>true → 🏆, false → 🔒 (Achievement-Status-Icon).</summary>
    public static readonly IValueConverter UnlockIcon =
        new FuncValueConverter<bool, string>(unlocked => unlocked ? "🏆" : "🔒");
}
