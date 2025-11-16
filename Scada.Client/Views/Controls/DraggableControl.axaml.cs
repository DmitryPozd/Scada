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
    private bool _isResizing;
    private Point _resizeStartPoint;
    private Size _resizeStartSize;
    private Border? _resizeHandle;

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
        
        _resizeHandle = this.FindControl<Border>("ResizeHandle");
        if (_resizeHandle != null)
        {
            _resizeHandle.PointerPressed += OnResizeHandlePressed;
            _resizeHandle.PointerMoved += OnResizeHandleMoved;
            _resizeHandle.PointerReleased += OnResizeHandleReleased;
            
            // Для отладки - показываем ручку сразу, если элемент изменяемый
            if (IsResizable())
            {
                System.Diagnostics.Debug.WriteLine($"DraggableControl: Content is {Content?.GetType().Name}, showing resize handle");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("DraggableControl: ResizeHandle not found!");
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        var isResizable = IsResizable();
        System.Diagnostics.Debug.WriteLine($"DraggableControl.OnPointerEntered: Content={Content?.GetType().Name}, IsResizable={isResizable}, Handle={_resizeHandle != null}");
        if (_resizeHandle != null && isResizable)
        {
            _resizeHandle.IsVisible = true;
            System.Diagnostics.Debug.WriteLine("  -> Showing resize handle");
        }
    }

    private bool IsResizable()
    {
        return Content is CustomIndicator or DisplayControl or ImageControl or ImageButton;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_resizeHandle != null && !_isResizing)
        {
            _resizeHandle.IsVisible = false;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdatePosition();
    }

    private void OnResizeHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var size = GetElementSize();
            if (size.HasValue)
            {
                _isResizing = true;
                _resizeStartPoint = e.GetPosition(this.Parent as Visual);
                _resizeStartSize = size.Value;
                e.Pointer.Capture(_resizeHandle);
                e.Handled = true;
            }
        }
    }

    private Size? GetElementSize()
    {
        return Content switch
        {
            CustomIndicator ci => new Size(ci.IndicatorWidth, ci.IndicatorHeight),
            DisplayControl dc => new Size(dc.Width, dc.Height),
            ImageControl ic => new Size(ic.Width, ic.Height),
            ImageButton ib => new Size(ib.ButtonWidth, ib.ButtonHeight),
            _ => null
        };
    }

    private void OnResizeHandleMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizing)
        {
            var currentPoint = e.GetPosition(this.Parent as Visual);
            var delta = currentPoint - _resizeStartPoint;

            var newWidth = Math.Max(50, _resizeStartSize.Width + delta.X);
            var newHeight = Math.Max(50, _resizeStartSize.Height + delta.Y);

            SetElementSize(newWidth, newHeight);
            
            e.Handled = true;
        }
    }

    private void SetElementSize(double width, double height)
    {
        switch (Content)
        {
            case CustomIndicator ci:
                ci.IndicatorWidth = width;
                ci.IndicatorHeight = height;
                break;
            case DisplayControl dc:
                dc.Width = width;
                dc.Height = height;
                break;
            case ImageControl ic:
                ic.Width = width;
                ic.Height = height;
                break;
            case ImageButton ib:
                ib.ButtonWidth = width;
                ib.ButtonHeight = height;
                break;
        }
    }

    private void OnResizeHandleReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
            
            if (_resizeHandle != null && !IsPointerOver)
            {
                _resizeHandle.IsVisible = false;
            }
            
            RaiseEvent(new RoutedEventArgs(SizeChangedCustomEvent));
            e.Handled = true;
        }
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

    // Событие для уведомления об изменении размера
    public static readonly RoutedEvent<RoutedEventArgs> SizeChangedCustomEvent =
        RoutedEvent.Register<DraggableControl, RoutedEventArgs>(
            nameof(SizeChangedCustom), RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs> SizeChangedCustom
    {
        add => AddHandler(SizeChangedCustomEvent, value);
        remove => RemoveHandler(SizeChangedCustomEvent, value);
    }
}
