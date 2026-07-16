using Avalonia.Input;
using Avalonia.Interactivity;

namespace Amtsschimmel.Views;

public sealed partial class VictoryWindow : ChromeWindow
{
    public VictoryWindow() => InitializeComponent();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e) => Close();
}
