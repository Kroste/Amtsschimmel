using Avalonia.Controls;

namespace Amtsschimmel.Views;

/// <summary>
/// Custom-Chrome nach Avalonia-12-Konvention (Projektstandard):
/// BorderOnly (NICHT None — sonst fehlen die nativen Resize-Griffe),
/// Client-Area bis in die Dekoration ausgedehnt, immer resizable.
/// </summary>
public class ChromeWindow : Window
{
    protected ChromeWindow()
    {
        WindowDecorations = WindowDecorations.BorderOnly;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;
        CanResize = true;
        Background = null;
    }
}
