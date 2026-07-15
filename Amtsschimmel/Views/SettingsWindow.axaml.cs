using Amtsschimmel.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Amtsschimmel.Views;

public sealed partial class SettingsWindow : ChromeWindow
{
    public SettingsWindow() => InitializeComponent();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private async void OnCopyExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && !string.IsNullOrEmpty(vm.ExportText)
            && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(vm.ExportText);
        }
    }
}
