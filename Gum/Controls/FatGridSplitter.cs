using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

public class FatHitGridSplitter : GridSplitter
{
    public static readonly DependencyProperty HitPaddingProperty =
        DependencyProperty.Register(
            nameof(HitPadding),
            typeof(double),
            typeof(FatHitGridSplitter),
            new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Extra pixels to accept grabs beyond the splitter’s actual arranged bounds
    /// (applied on both sides; 6 means ~12px total).
    /// </summary>
    public double HitPadding
    {
        get => (double)GetValue(HitPaddingProperty);
        set => SetValue(HitPaddingProperty, value);
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        var p = hitTestParameters.HitPoint;

        // Expand the clickable rect by HitPadding on all sides
        var rect = new Rect(-HitPadding, -HitPadding, ActualWidth + HitPadding * 2, ActualHeight + HitPadding * 2);

        if (rect.Contains(p))
            return new PointHitTestResult(this, p);

        return null!;
    }

    // Make sure mouse cursor updates when hovering the padded zone
    protected override Geometry GetLayoutClip(Size layoutSlotSize) => null;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // Pick a cursor based on direction; GridSplitter does this
        // automatically inside bounds, but we also want it in the padded zone.
        if (IsMouseOverPaddedArea(e))
        {
            Cursor = (ResizeDirection == GridResizeDirection.Rows)
                ? Cursors.SizeNS
                : Cursors.SizeWE;
        }
        else
        {
            ClearValue(CursorProperty);
        }
    }

    private bool IsMouseOverPaddedArea(MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        var rect = new Rect(-HitPadding, -HitPadding, ActualWidth + HitPadding * 2, ActualHeight + HitPadding * 2);
        return rect.Contains(p);
    }
}