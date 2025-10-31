using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ReactiveUI;
using Scada.Client.Models;

namespace Scada.Client.Views.Controls;

public partial class CoilReadButton : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<CoilReadButton, string>(nameof(Label), "Катушка");

    public static readonly StyledProperty<ushort> CoilAddressProperty =
        AvaloniaProperty.Register<CoilReadButton, ushort>(nameof(CoilAddress));

    public static readonly StyledProperty<bool?> CoilValueProperty =
        AvaloniaProperty.Register<CoilReadButton, bool?>(nameof(CoilValue));

    public static readonly StyledProperty<ICommand?> ReadCommandProperty =
        AvaloniaProperty.Register<CoilReadButton, ICommand?>(nameof(ReadCommand));

    public static readonly StyledProperty<IEnumerable<TagDefinition>?> AvailableTagsProperty =
        AvaloniaProperty.Register<CoilReadButton, IEnumerable<TagDefinition>?>(nameof(AvailableTags));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ushort CoilAddress
    {
        get => GetValue(CoilAddressProperty);
        set => SetValue(CoilAddressProperty, value);
    }

    public bool? CoilValue
    {
        get => GetValue(CoilValueProperty);
        set => SetValue(CoilValueProperty, value);
    }

    public ICommand? ReadCommand
    {
        get => GetValue(ReadCommandProperty);
        set => SetValue(ReadCommandProperty, value);
    }

    public IEnumerable<TagDefinition>? AvailableTags
    {
        get => GetValue(AvailableTagsProperty);
        set => SetValue(AvailableTagsProperty, value);
    }

    public CoilReadButton()
    {
        InitializeComponent();
        
        // Subscribe to CoilValue changes to update visual state
        this.GetObservable(CoilValueProperty).Subscribe(UpdateVisualState);
    }

    private void UpdateVisualState(bool? value)
    {
        // Find the status indicator elements
        var border = this.FindControl<Border>("StatusBorder");
        var textBlock = this.FindControl<TextBlock>("StatusText");
        
        if (border != null && textBlock != null)
        {
            if (value.HasValue)
            {
                border.Background = value.Value ? Brushes.Green : Brushes.Red;
                textBlock.Text = value.Value ? "1" : "0";
            }
            else
            {
                border.Background = Brushes.Gray;
                textBlock.Text = "?";
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ShowTagSelectionDialog();
            e.Handled = true;
            return;
        }
        
        base.OnPointerReleased(e);
    }

    private async void ShowTagSelectionDialog()
    {
        var tags = AvailableTags?.Where(t => t.Register == RegisterType.Coils)?.ToList();
        
        if (tags == null || !tags.Any())
        {
            // Fallback to address input dialog
            await ShowAddressInputDialog();
            return;
        }

        var dialog = new Window
        {
            Title = "Выбор тега катушки",
            Width = 350,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
        
        var combo = new ComboBox
        {
            ItemsSource = tags,
            SelectedItem = tags.FirstOrDefault(t => t.Address == CoilAddress),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };
        
        combo.ItemTemplate = new FuncDataTemplate<TagDefinition>((tag, _) =>
            new TextBlock { Text = $"{tag.Name} (адрес: {tag.Address})" });

        var buttonsPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var okButton = new Button { Content = "OK", MinWidth = 80 };
        var cancelButton = new Button { Content = "Отмена", MinWidth = 80 };

        okButton.Click += (_, _) =>
        {
            if (combo.SelectedItem is TagDefinition selectedTag)
            {
                CoilAddress = selectedTag.Address;
            }
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);

        panel.Children.Add(new TextBlock { Text = "Выберите тег катушки:" });
        panel.Children.Add(combo);
        panel.Children.Add(buttonsPanel);

        dialog.Content = panel;

        if (TopLevel.GetTopLevel(this) is Window parentWindow)
        {
            await dialog.ShowDialog(parentWindow);
        }
    }

    private async Task ShowAddressInputDialog()
    {
        var dialog = new Window
        {
            Title = "Ввод адреса катушки",
            Width = 300,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
        
        var numericUpDown = new NumericUpDown
        {
            Value = CoilAddress,
            Minimum = 0,
            Maximum = 65535,
            Increment = 1
        };

        var buttonsPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var okButton = new Button { Content = "OK", MinWidth = 80 };
        var cancelButton = new Button { Content = "Отмена", MinWidth = 80 };

        okButton.Click += (_, _) =>
        {
            CoilAddress = (ushort)numericUpDown.Value;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);

        panel.Children.Add(new TextBlock { Text = "Введите адрес катушки:" });
        panel.Children.Add(numericUpDown);
        panel.Children.Add(buttonsPanel);

        dialog.Content = panel;

        if (TopLevel.GetTopLevel(this) is Window parentWindow)
        {
            await dialog.ShowDialog(parentWindow);
        }
    }
}