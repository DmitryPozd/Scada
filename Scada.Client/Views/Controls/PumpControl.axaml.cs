using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Scada.Client.Views.Controls;

public partial class PumpControl : UserControl
{
    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<PumpControl, bool>(nameof(IsRunning));

    public static readonly StyledProperty<bool> HasAlarmProperty =
        AvaloniaProperty.Register<PumpControl, bool>(nameof(HasAlarm));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<PumpControl, string>(nameof(Label), defaultValue: "Насос");

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public bool HasAlarm
    {
        get => GetValue(HasAlarmProperty);
        set => SetValue(HasAlarmProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public PumpControl()
    {
        InitializeComponent();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsRightButtonPressed)
        {
            ShowContextMenu();
            e.Handled = true;
        }
    }

    private async void ShowContextMenu()
    {
        var dialog = new Window
        {
            Title = "Настройки насоса",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        var labelTextBlock = new TextBlock { Text = "Название насоса:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название насоса"
        };

        var buttons = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 10, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
        };
        
        var okButton = new Button { Content = "OK", Width = 80 };
        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(labelInput.Text))
            {
                Label = labelInput.Text;
            }
            dialog.Close();
        };
        
        var cancelButton = new Button { Content = "Отмена", Width = 80 };
        cancelButton.Click += (s, e) => dialog.Close();

        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);
        stack.Children.Add(buttons);
        
        dialog.Content = stack;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }
}

public class StatusTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool alarm && values[1] is bool running)
        {
            if (alarm) return "Авария";
            return running ? "Работает" : "Остановлен";
        }
        return string.Empty;
    }
}

public class RunningFillConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Brushes.MediumSeaGreen : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class AlarmStrokeBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Brushes.IndianRed : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class AlarmStrokeThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? 3d : 0d;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
