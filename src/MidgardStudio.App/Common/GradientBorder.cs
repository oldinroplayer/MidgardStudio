using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace MidgardStudio.App.Common;

/// <summary>
/// When <c>Enabled</c> is true, gives a control (or border) an "electric" animated edge — the brand
/// gradient circles the border with a bright spark travelling through it, plus a pulsing glow (à la
/// reactbits' electric border, in our theme colors). When false it reverts to solid black with no glow.
/// Applied to a thin outer "ring" border (an inner black border covers the centre) so only the edge
/// shows. Built in code as local values so the animation survives styling and never targets a frozen brush.
/// </summary>
public static class GradientBorder
{
    private static readonly Color C1 = (Color)ColorConverter.ConvertFromString("#8D2DF2")!; // brand violet
    private static readonly Color C2 = (Color)ColorConverter.ConvertFromString("#D916FB")!; // brand magenta
    private static readonly Color C3 = (Color)ColorConverter.ConvertFromString("#B497CF")!; // brand lavender
    private static readonly Color Spark = (Color)ColorConverter.ConvertFromString("#F4E4FF")!; // bright spark

    private static readonly Brush Idle = Frozen(new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)));

    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(GradientBorder), new PropertyMetadata(false, OnChanged));

    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);

    /// <summary>When true (default), the animated edge also gets a pulsing glow halo. Set false for the
    /// rotating gradient edge alone — used by the validation severity badges, where a per-row glow would both
    /// wash out the severity colour and stack dozens of drop-shadows in the list.</summary>
    public static readonly DependencyProperty GlowProperty = DependencyProperty.RegisterAttached(
        "Glow", typeof(bool), typeof(GradientBorder), new PropertyMetadata(true, OnChanged));

    public static bool GetGlow(DependencyObject o) => (bool)o.GetValue(GlowProperty);
    public static void SetGlow(DependencyObject o, bool value) => o.SetValue(GlowProperty, value);

    /// <summary>Tints the animated edge to a single accent colour instead of the brand violet gradient
    /// (e.g. red/amber/blue per validation severity). Transparent (default) keeps the brand gradient.</summary>
    public static readonly DependencyProperty AccentProperty = DependencyProperty.RegisterAttached(
        "Accent", typeof(Color), typeof(GradientBorder), new PropertyMetadata(Colors.Transparent, OnChanged));

    public static Color GetAccent(DependencyObject o) => (Color)o.GetValue(AccentProperty);
    public static void SetAccent(DependencyObject o, Color value) => o.SetValue(AccentProperty, value);

    // One animated brush per distinct accent, SHARED across every element that uses it (e.g. all error badges
    // share one brush) — so a long virtualized list runs ~4 animation clocks, not one per visible row, and a
    // recycled container reuses the brush instead of allocating a new one on every scroll step.
    private static readonly System.Collections.Generic.Dictionary<int, Brush> BrushCache = new();

    private static Brush RotatingFor(Color accent)
    {
        int key = accent.A == 0 ? -1 : (accent.R << 16) | (accent.G << 8) | accent.B;
        if (BrushCache.TryGetValue(key, out var b)) return b;
        b = CreateRotating(accent);
        BrushCache[key] = b;
        return b;
    }

    // Any of the three properties changing re-applies, so the result is correct regardless of attribute order.
    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        bool on = GetEnabled(d);
        Brush background = on ? RotatingFor(GetAccent(d)) : Idle;
        Effect? glow = on && GetGlow(d) ? CreateGlow() : null;

        // The target is a Border (a Decorator, NOT a Control), so we can't assume Control here.
        switch (d)
        {
            case Border b: b.Background = background; b.Effect = glow; break;
            case Panel p: p.Background = background; p.Effect = glow; break;
            case Control c: c.Background = background; c.Effect = glow; break;
        }
    }

    private static Brush CreateRotating(Color accent)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            SpreadMethod = GradientSpreadMethod.Reflect,
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
        };
        if (accent.A == 0)
        {
            // Brand gradient: two bright sparks racing through it read as a current arcing around the edge.
            brush.GradientStops.Add(new GradientStop(C1, 0.00));
            brush.GradientStops.Add(new GradientStop(C2, 0.18));
            brush.GradientStops.Add(new GradientStop(Spark, 0.27));
            brush.GradientStops.Add(new GradientStop(C2, 0.36));
            brush.GradientStops.Add(new GradientStop(C3, 0.50));
            brush.GradientStops.Add(new GradientStop(C2, 0.64));
            brush.GradientStops.Add(new GradientStop(Spark, 0.73));
            brush.GradientStops.Add(new GradientStop(C2, 0.82));
            brush.GradientStops.Add(new GradientStop(C1, 1.00));
        }
        else
        {
            // Single-accent variant: dark→accent→bright spark→accent→dark, so the edge reads as the accent
            // colour (e.g. the severity) while still arcing.
            Color dark = Mix(accent, Colors.Black, 0.45);
            Color spark = Mix(accent, Colors.White, 0.7);
            brush.GradientStops.Add(new GradientStop(dark, 0.00));
            brush.GradientStops.Add(new GradientStop(accent, 0.20));
            brush.GradientStops.Add(new GradientStop(spark, 0.30));
            brush.GradientStops.Add(new GradientStop(accent, 0.42));
            brush.GradientStops.Add(new GradientStop(dark, 0.55));
            brush.GradientStops.Add(new GradientStop(accent, 0.70));
            brush.GradientStops.Add(new GradientStop(spark, 0.80));
            brush.GradientStops.Add(new GradientStop(accent, 0.90));
            brush.GradientStops.Add(new GradientStop(dark, 1.00));
        }

        var rotate = new RotateTransform(0, 0.5, 0.5);
        brush.RelativeTransform = rotate;
        rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360,
            new Duration(TimeSpan.FromSeconds(2.0))) { RepeatBehavior = RepeatBehavior.Forever });
        return brush;
    }

    private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    private static Effect CreateGlow()
    {
        var glow = new DropShadowEffect
        {
            Color = C2,         // brand magenta aura
            ShadowDepth = 0,
            BlurRadius = 9,
            Opacity = 0.75,
        };
        // A quick flicker on the glow sells the "electric" energy.
        var pulse = new Duration(TimeSpan.FromSeconds(0.85));
        glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
            new DoubleAnimation(6, 16, pulse) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        glow.BeginAnimation(DropShadowEffect.OpacityProperty,
            new DoubleAnimation(0.55, 0.95, pulse) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        return glow;
    }

    private static Brush Frozen(Brush b) { b.Freeze(); return b; }
}
