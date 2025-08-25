using System.Windows;

namespace Gum.Themes;

public sealed class AppScale : DependencyObject
{
    public double BaseFontSize
    {
        get => (double)GetValue(BaseFontSizeProperty);
        set => SetValue(BaseFontSizeProperty, value);
    }

    public static readonly DependencyProperty BaseFontSizeProperty =
        DependencyProperty.Register(nameof(BaseFontSize), typeof(double), typeof(AppScale),
            new PropertyMetadata(12.0, OnBaseChanged));

    // Text tokens (add any you need)
    public double Body { get => (double)GetValue(BodyProperty); private set => SetValue(BodyProperty, value); }
    public double Caption { get => (double)GetValue(CaptionProperty); private set => SetValue(CaptionProperty, value); }
    public double H1 { get => (double)GetValue(H1Property); private set => SetValue(H1Property, value); }
    public double H2 { get => (double)GetValue(H2Property); private set => SetValue(H2Property, value); }

    // Icon tokens
    public double IconInline { get => (double)GetValue(IconInlineProperty); private set => SetValue(IconInlineProperty, value); }
    public double IconButton { get => (double)GetValue(IconButtonProperty); private set => SetValue(IconButtonProperty, value); }

    public static readonly DependencyProperty BodyProperty = DP(nameof(Body));
    public static readonly DependencyProperty CaptionProperty = DP(nameof(Caption));
    public static readonly DependencyProperty H1Property = DP(nameof(H1));
    public static readonly DependencyProperty H2Property = DP(nameof(H2));
    public static readonly DependencyProperty IconInlineProperty = DP(nameof(IconInline));
    public static readonly DependencyProperty IconButtonProperty = DP(nameof(IconButton));

    static DependencyProperty DP(string name) =>
        DependencyProperty.Register(name, typeof(double), typeof(AppScale), new PropertyMetadata(0d));

    static void OnBaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var s = (AppScale)d;
        var b = s.BaseFontSize;

        // Text scale (pick factors you like; examples shown)
        s.Body = b;             // base body
        s.Caption = b * 0.85;      // small
        s.H2 = b * 1.25;      // subheader
        s.H1 = b * 1.6;       // header

        // Icon scale tokens tied to *current* base
        s.IconInline = b * 1.16;   // inline with text
        s.IconButton = b * 1.667;  // on icon-only or button icons
    }
}