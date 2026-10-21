using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace Rat6.App.Controls;

public sealed class ProfileDragArea : Border
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ProfileDragArea, double>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ProfileDragArea, int>(nameof(SelectedIndex));

    public static readonly StyledProperty<int> ProfileCountProperty =
        AvaloniaProperty.Register<ProfileDragArea, int>(nameof(ProfileCount), 4);

    public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<ProfileDragArea, ICommand?>(nameof(SelectionChangedCommand));

    private const double HorizontalPadding = 14;

    private bool _dragging;

    public ProfileDragArea()
    {
        Background = Brushes.Transparent;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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

    public ICommand? SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            var value = change.GetNewValue<double>();
            if (double.IsFinite(value))
            {
                var index = (int)Math.Clamp(Math.Round(value), 0, Math.Max(0, ProfileCount - 1));
                if (SelectedIndex != index)
                {
                    SetCurrentValue(SelectedIndexProperty, index);
                }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragging = true;
        e.Pointer.Capture(this);
        Focus();
        SetFromPosition(point.Position);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_dragging)
        {
            return;
        }

        SetFromPosition(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        SetFromPosition(e.GetPosition(this));
        CommitSelection();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (_dragging)
        {
            _dragging = false;
            CommitSelection();
        }

        base.OnPointerCaptureLost(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.Delta.Y == 0)
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        SetIndex(SelectedIndex + (e.Delta.Y > 0 ? -1 : 1));
        CommitSelection();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is Key.Left or Key.Down)
        {
            SetIndex(SelectedIndex - 1);
            CommitSelection();
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.Up)
        {
            SetIndex(SelectedIndex + 1);
            CommitSelection();
            e.Handled = true;
        }
    }

    private void SetFromPosition(Point position)
    {
        var count = Math.Max(2, ProfileCount);
        var left = HorizontalPadding;
        var right = Bounds.Width - HorizontalPadding;
        if (right <= left)
        {
            return;
        }

        var normalized = Math.Clamp((position.X - left) / (right - left), 0, 1);
        SetIndex((int)Math.Round(normalized * (count - 1)));
    }

    private void SetIndex(int index)
    {
        var maximum = Math.Max(0, ProfileCount - 1);
        var clamped = Math.Clamp(index, 0, maximum);
        SetCurrentValue(SelectedIndexProperty, clamped);
        SetCurrentValue(ValueProperty, (double)clamped);
    }

    private void CommitSelection()
    {
        if (SelectionChangedCommand is { } command && command.CanExecute(SelectedIndex))
        {
            command.Execute(SelectedIndex);
        }
    }
}
