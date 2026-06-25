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
        "Enabled", typeof(bool), typeof(GradientBorder), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        bool on = e.NewValue is true;
        Brush background = on ? CreateRotating() : Idle;
        Effect? glow = on ? CreateGlow() : null;

        // The target is a Border (a Decorator, NOT a Control), so we can't assume Control here.
        switch (d)
        {
            case Border b: b.Background = background; b.Effect = glow; break;
            case Panel p: p.Background = background; p.Effect = glow; break;
            case Control c: c.Background = background; c.Effect = glow; break;
        }
    }

    private static Brush CreateRotating()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            SpreadMethod = GradientSpreadMethod.Reflect,
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
        };
        // Two bright sparks racing through the brand gradient read as a current arcing around the edge.
        brush.GradientStops.Add(new GradientStop(C1, 0.00));
        brush.GradientStops.Add(new GradientStop(C2, 0.18));
        brush.GradientStops.Add(new GradientStop(Spark, 0.27));
        brush.GradientStops.Add(new GradientStop(C2, 0.36));
        brush.GradientStops.Add(new GradientStop(C3, 0.50));
        brush.GradientStops.Add(new GradientStop(C2, 0.64));
        brush.GradientStops.Add(new GradientStop(Spark, 0.73));
        brush.GradientStops.Add(new GradientStop(C2, 0.82));
        brush.GradientStops.Add(new GradientStop(C1, 1.00));

        var rotate = new RotateTransform(0, 0.5, 0.5);
        brush.RelativeTransform = rotate;
        rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360,
            new Duration(TimeSpan.FromSeconds(2.0))) { RepeatBehavior = RepeatBehavior.Forever });
        return brush;
    }

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
