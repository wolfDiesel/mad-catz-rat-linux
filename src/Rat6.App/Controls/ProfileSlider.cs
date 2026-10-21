using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Rat6.App.Controls;

public sealed class ProfileSlider : Control
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ProfileSlider, int>(nameof(SelectedIndex));

    public static readonly StyledProperty<int> ProfileCountProperty =
        AvaloniaProperty.Register<ProfileSlider, int>(nameof(ProfileCount), 4);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<ProfileSlider, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> DotBrushProperty =
        AvaloniaProperty.Register<ProfileSlider, IBrush?>(nameof(DotBrush));

    public static readonly StyledProperty<IBrush?> ThumbBrushProperty =
        AvaloniaProperty.Register<ProfileSlider, IBrush?>(nameof(ThumbBrush));

    public static readonly StyledProperty<IBrush?> ThumbBorderBrushProperty =
        AvaloniaProperty.Register<ProfileSlider, IBrush?>(nameof(ThumbBorderBrush));

    private const double HorizontalPadding = 14;
    private const double DotRadius = 3;
    private const double ThumbRadius = 7;

    static ProfileSlider()
    {
        AffectsRender<ProfileSlider>(
            SelectedIndexProperty,
            ProfileCountProperty,
            TrackBrushProperty,
            DotBrushProperty,
            ThumbBrushProperty,
            ThumbBorderBrushProperty);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public int ProfileCount
    {
        get => GetValue(ProfileCountProperty);
        set => SetValue(ProfileCountProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? DotBrush
    {
        get => GetValue(DotBrushProperty);
        set => SetValue(DotBrushProperty, value);
    }

    public IBrush? ThumbBrush
    {
        get => GetValue(ThumbBrushProperty);
        set => SetValue(ThumbBrushProperty, value);
    }

    public IBrush? ThumbBorderBrush
    {
        get => GetValue(ThumbBorderBrushProperty);
        set => SetValue(ThumbBorderBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        if (width <= HorizontalPadding * 2)
        {
            return;
        }

        var centerY = Bounds.Height / 2;
        var left = HorizontalPadding;
        var right = width - HorizontalPadding;
        var count = Math.Max(2, ProfileCount);
        var trackBrush = TrackBrush ?? Brushes.Gray;
        var dotBrush = DotBrush ?? trackBrush;
        var thumbBrush = ThumbBrush ?? Brushes.White;
        var thumbBorderBrush = ThumbBorderBrush ?? Brushes.Black;

        context.DrawLine(
            new Pen(trackBrush, 2, lineCap: PenLineCap.Round),
            new Point(left, centerY),
            new Point(right, centerY));

        for (var index = 0; index < count; index++)
        {
            var x = left + (right - left) * index / (count - 1);
            context.DrawEllipse(dotBrush, null, new Point(x, centerY), DotRadius, DotRadius);
        }

        var selected = Math.Clamp(SelectedIndex, 0, count - 1);
        var thumbX = left + (right - left) * selected / (count - 1);
        context.DrawEllipse(
            thumbBrush,
            new Pen(thumbBorderBrush, 2),
            new Point(thumbX, centerY),
            ThumbRadius,
            ThumbRadius);
    }
}
