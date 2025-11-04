using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Scada.Client.Views.Controls;

public partial class SensorIndicator : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SensorIndicator, double>(nameof(Value), 0d);

    public static readonly StyledProperty<double?> ThresholdLowProperty =
        AvaloniaProperty.Register<SensorIndicator, double?>(nameof(ThresholdLow));

    public static readonly StyledProperty<double?> ThresholdHighProperty =
        AvaloniaProperty.Register<SensorIndicator, double?>(nameof(ThresholdHigh));

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<SensorIndicator, string>(nameof(Unit), defaultValue: string.Empty);

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<SensorIndicator, string>(nameof(Label), defaultValue: "Датчик");

    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double? ThresholdLow { get => GetValue(ThresholdLowProperty); set => SetValue(ThresholdLowProperty, value); }
    public double? ThresholdHigh { get => GetValue(ThresholdHighProperty); set => SetValue(ThresholdHighProperty, value); }
    public string Unit { get => GetValue(UnitProperty); set => SetValue(UnitProperty, value); }
    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public SensorIndicator()
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
            Title = "Настройки датчика",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            MaxWidth = 500,
            MaxHeight = 400
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        var labelTextBlock = new TextBlock { Text = "Название датчика:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название датчика"
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

public class SensorValueBrushConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 3 && values[0] is double val)
        {
            double? low = values[1] as double?;
            double? high = values[2] as double?;
            if (low.HasValue && val < low.Value) return Brushes.CadetBlue;
            if (high.HasValue && val > high.Value) return Brushes.IndianRed;
            return Brushes.SeaGreen;
        }
        return Brushes.SeaGreen;
    }
}

public class SensorStatusTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 3 && values[0] is double val)
        {
            double? low = values[1] as double?;
            double? high = values[2] as double?;
            if (low.HasValue && val < low.Value) return "Ниже нормы";
            if (high.HasValue && val > high.Value) return "Выше нормы";
            return "В норме";
        }
        return string.Empty;
    }
}
