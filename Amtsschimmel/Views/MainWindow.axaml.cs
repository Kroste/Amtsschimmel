using Amtsschimmel.ViewModels;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Amtsschimmel.Views;

public sealed partial class MainWindow : ChromeWindow
{
    private static readonly Random Rng = new();
    private static readonly IBrush ManualParticleBrush = new SolidColorBrush(Color.Parse("#8FD4A8"));
    private static readonly IBrush AutoParticleBrush = new SolidColorBrush(Color.Parse("#9FB7D9"));

    private readonly DispatcherTimer _autoStampTimer;
    private double _autoParticleAccumulator;

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
        // Partikel für den Stempelautomaten: 4×/s prüfen, Rate über Akkumulator abbilden.
        _autoStampTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, OnAutoStampTimer);
        _autoStampTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoStampTimer.Stop();
        base.OnClosed(e);
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

    /// <summary>Klick-Feedback: "+X" steigt vom Stempel-Button auf (Command bucht, Click animiert).</summary>
    private void OnStampClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            SpawnParticle("+" + vm.ClickPowerText, ManualParticleBrush);
        }
    }

    /// <summary>Der Stempelautomat stempelt sichtbar mit: Partikel gemäß Auto-Klick-Rate (max. 4/s).</summary>
    private void OnAutoStampTimer(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.AutoClicksPerSecondValue <= 0
            || WindowState == WindowState.Minimized)
        {
            _autoParticleAccumulator = 0;
            return;
        }
        _autoParticleAccumulator += Math.Min(vm.AutoClicksPerSecondValue, 4) * 0.25;
        while (_autoParticleAccumulator >= 1)
        {
            _autoParticleAccumulator -= 1;
            SpawnParticle(vm.AutoParticleText, AutoParticleBrush);
        }
    }

    private async void SpawnParticle(string text, IBrush brush)
    {
        var canvas = ClickParticleCanvas;
        if (canvas.Bounds.Width <= 0 || canvas.Children.Count > 30) // Spam-Schutz
        {
            return;
        }

        var label = new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.Bold,
            FontSize = 18,
            Foreground = brush,
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
