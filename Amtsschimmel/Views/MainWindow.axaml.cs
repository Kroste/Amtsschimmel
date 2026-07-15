using Avalonia.Controls;
using Avalonia.Input;

namespace Amtsschimmel.Views;

public sealed partial class MainWindow : ChromeWindow
{
    public MainWindow() => InitializeComponent();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnTitleBarDoubleTapped(object? sender, TappedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
