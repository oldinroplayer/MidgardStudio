using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MidgardStudio.App.Views;

/// <summary>
/// Animated, branded splash shown on every launch while the workspace loads. It runs on its own dedicated
/// UI thread (see <c>App.OnStartup</c>) so its animations stay smooth even while the main thread is busy
/// loading the databases. Call <see cref="BeginShutdown"/> to fade it out and tear down its dispatcher.
/// </summary>
public partial class SplashWindow : Window
{
    private bool _shuttingDown;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyTitleGradient();
    }

    /// <summary>Builds the looping logo-coloured gradient on the title (purple → magenta → lavender), the same
    /// effect the in-app headings use, but self-contained so it runs on this window's own thread.</summary>
    private void ApplyTitleGradient()
    {
        var c1 = (Color)ColorConverter.ConvertFromString("#9A4DF5")!;
        var c2 = (Color)ColorConverter.ConvertFromString("#D916FB")!;
        var c3 = (Color)ColorConverter.ConvertFromString("#C9A9F2")!;

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            SpreadMethod = GradientSpreadMethod.Repeat,
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
        };
        brush.GradientStops.Add(new GradientStop(c1, 0.00));
        brush.GradientStops.Add(new GradientStop(c2, 0.25));
        brush.GradientStops.Add(new GradientStop(c3, 0.50));
        brush.GradientStops.Add(new GradientStop(c2, 0.75));
        brush.GradientStops.Add(new GradientStop(c1, 1.00));

        var slide = new TranslateTransform(0, 0);
        brush.RelativeTransform = slide;
        TitleText.Foreground = brush;

        slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, 1,
            new Duration(TimeSpan.FromSeconds(4))) { RepeatBehavior = RepeatBehavior.Forever });
    }

    /// <summary>Fades the splash out, then closes it and shuts down this thread's dispatcher so the background
    /// splash thread can exit cleanly. Safe to call from the main thread via <c>splash.Dispatcher.Invoke</c>.</summary>
    public void BeginShutdown()
    {
        if (_shuttingDown) return; // idempotent — safe even if called more than once
        _shuttingDown = true;

        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(380)));
        fade.Completed += (_, _) =>
        {
            Close();
            Dispatcher.InvokeShutdown();
        };
        BeginAnimation(OpacityProperty, fade);
    }
}
