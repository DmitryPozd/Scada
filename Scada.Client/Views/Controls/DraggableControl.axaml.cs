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
    private bool _wasDragged; // Флаг: было ли реальное перемещение

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
            _wasDragged = false;
            _dragStartPoint = e.GetPosition(this.Parent as Visual);
            // НЕ захватываем указатель и НЕ блокируем событие сразу
            // Это позволит дочерним элементам (кнопкам) получить событие
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Проверяем, что левая кнопка нажата
        var properties = e.GetCurrentPoint(this).Properties;
        if (_isDragging && properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this.Parent as Visual);
            var delta = currentPoint - _dragStartPoint;

            // Если переместились хотя бы на 2 пикселя - начинаем перетаскивание
            if (!_wasDragged && (Math.Abs(delta.X) > 2 || Math.Abs(delta.Y) > 2))
            {
                _wasDragged = true;
                // Теперь захватываем указатель для плавного перетаскивания
                e.Pointer.Capture(this);
            }

            if (_wasDragged)
            {
                X += delta.X;
                Y += delta.Y;
                _dragStartPoint = currentPoint;
                e.Handled = true;
            }
        }
        else if (_isDragging)
        {
            // Если _isDragging = true, но кнопка не нажата - сбрасываем состояние
            _isDragging = false;
            _wasDragged = false;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            _isDragging = false;
            
            // Освобождаем захват указателя, если он был
            if (e.Pointer.Captured == this)
            {
                e.Pointer.Capture(null);
            }
            
            // Если было реальное перемещение - блокируем событие и сохраняем позицию
            if (_wasDragged)
            {
                e.Handled = true;
                RaiseEvent(new RoutedEventArgs(PositionChangedEvent));
            }
            // Если не было перемещения - НЕ блокируем событие,
            // чтобы оно дошло до кнопки
            
            _wasDragged = false;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        
        // Сбрасываем состояние перетаскивания
        _isDragging = false;
        _wasDragged = false;
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
