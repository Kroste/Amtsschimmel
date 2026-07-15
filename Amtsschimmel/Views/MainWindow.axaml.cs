using Amtsschimmel.ViewModels;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace Amtsschimmel.Views;

public sealed partial class MainWindow : ChromeWindow
{
    private static readonly Random Rng = new();
    private static readonly IBrush ParticleBrush = new SolidColorBrush(Color.Parse("#8FD4A8"));

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
    }

    /// <summary>Positioniert das Goldene Formular zufällig, sobald es erscheint.</summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsGoldenFormVisible)
            || sender is not MainWindowViewModel { IsGoldenFormVisible: true })
        {
            return;
        }
        var maxX = Math.Max(41, (int)GoldenCanvas.Bounds.Width - 120);
        var maxY = Math.Max(41, (int)GoldenCanvas.Bounds.Height - 90);
        Canvas.SetLeft(GoldenFormButton, Rng.Next(40, maxX));
        Canvas.SetTop(GoldenFormButton, Rng.Next(40, maxY));
    }

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

    /// <summary>
    /// Klick-Feedback: lässt ein "+X" vom Stempel-Button aufsteigen und verblassen.
    /// Läuft zusätzlich zum StampCommand (Command bucht die Stempel, Click nur die Optik).
    /// </summary>
    private async void OnStampClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        var canvas = ClickParticleCanvas;
        if (canvas.Children.Count > 30) // Spam-Schutz bei Dauerklicken
        {
            return;
        }

        var label = new TextBlock
        {
            Text = "+" + vm.ClickPowerText,
            FontWeight = FontWeight.Bold,
            FontSize = 18,
            Foreground = ParticleBrush,
            IsHitTestVisible = false,
        };

        var startTop = canvas.Bounds.Height - 24;
        Canvas.SetLeft(label, canvas.Bounds.Width / 2 - 20 + Rng.Next(-60, 61));
        Canvas.SetTop(label, startTop);
        canvas.Children.Add(label);

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(900),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 1d),
                        new Setter(Canvas.TopProperty, startTop),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(OpacityProperty, 0d),
                        new Setter(Canvas.TopProperty, startTop - 80),
                    },
                },
            },
        };

        await animation.RunAsync(label);
        canvas.Children.Remove(label);
    }
}
