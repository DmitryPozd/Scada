using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Scada.Client.Views.Controls;

public partial class DraggableControl : ContentControl
{
    private Point _dragStartPoint;
    private bool _isDragging;

    public static readonly StyledProperty<double> XProperty =
        AvaloniaProperty.Register<DraggableControl, double>(nameof(X), 0.0);

    public static readonly StyledProperty<double> YProperty =
        AvaloniaProperty.Register<DraggableControl, double>(nameof(Y), 0.0);

    public double X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public double Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    static DraggableControl()
    {
        XProperty.Changed.AddClassHandler<DraggableControl>((control, e) => control.UpdatePosition());
        YProperty.Changed.AddClassHandler<DraggableControl>((control, e) => control.UpdatePosition());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePosition();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Canvas.SetLeft(this, X);
        Canvas.SetTop(this, Y);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this.Parent as Visual);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDragging && e.Pointer.Captured == this)
        {
            var currentPoint = e.GetPosition(this.Parent as Visual);
            var delta = currentPoint - _dragStartPoint;

            X += delta.X;
            Y += delta.Y;

            _dragStartPoint = currentPoint;
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;

            // Вызываем событие для сохранения позиции
            RaiseEvent(new RoutedEventArgs(PositionChangedEvent));
        }
    }

    // Событие для уведомления о завершении перемещения
    public static readonly RoutedEvent<RoutedEventArgs> PositionChangedEvent =
        RoutedEvent.Register<DraggableControl, RoutedEventArgs>(
            nameof(PositionChanged), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> PositionChanged
    {
        add => AddHandler(PositionChangedEvent, value);
        remove => RemoveHandler(PositionChangedEvent, value);
    }
}
