using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Scada.Client.Views.Controls;

public partial class ValveControl : UserControl
{
    public static readonly StyledProperty<double> OpenPercentProperty =
        AvaloniaProperty.Register<ValveControl, double>(nameof(OpenPercent), 0d);

    public static readonly StyledProperty<bool> HasAlarmProperty =
        AvaloniaProperty.Register<ValveControl, bool>(nameof(HasAlarm));

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ValveControl, string>(nameof(Label), defaultValue: "Клапан");

    public double OpenPercent
    {
        get => GetValue(OpenPercentProperty);
        set => SetValue(OpenPercentProperty, value);
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

    public ValveControl()
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
            Title = "Настройки клапана",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        var labelTextBlock = new TextBlock { Text = "Название клапана:", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox 
        { 
            Text = Label,
            Watermark = "Введите название клапана"
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
