using Avalonia.Input;
using Avalonia.Interactivity;

namespace Amtsschimmel.Views;

public sealed partial class InfoWindow : ChromeWindow
{
    public InfoWindow() => InitializeComponent();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
