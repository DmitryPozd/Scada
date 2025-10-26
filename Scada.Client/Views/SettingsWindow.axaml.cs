using Avalonia.Controls;
using Avalonia.Interactivity;
using Scada.Client.Models;
using Scada.Client.ViewModels;
using System.Linq;
using Avalonia;

namespace Scada.Client.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel vm)
        {
            // Validate tags: unique non-empty names; address range
            var errors = new System.Text.StringBuilder();
            var tags = vm.ConnectionConfig.Tags;
            // Trim names
            foreach (var t in tags)
            {
                t.Name = t.Name?.Trim() ?? string.Empty;
            }

            // Empty names
            var empties = tags.Where(t => string.IsNullOrWhiteSpace(t.Name)).ToList();
            if (empties.Count > 0)
            {
                errors.AppendLine($"Пустые имена тегов: {empties.Count} шт.");
            }

            // Duplicates
            var dups = tags.Where(t => !string.IsNullOrWhiteSpace(t.Name))
                           .GroupBy(t => t.Name, System.StringComparer.OrdinalIgnoreCase)
                           .Where(g => g.Count() > 1)
                           .Select(g => g.Key)
                           .ToList();
            if (dups.Count > 0)
            {
                errors.AppendLine("Дубликаты имён: " + string.Join(", ", dups));
            }

            // Address range
            var badAddr = tags.Where(t => t.Address > 65535).ToList();
            if (badAddr.Count > 0)
            {
                errors.AppendLine($"Некорректные адреса (должно быть 0..65535): {badAddr.Count} шт.");
            }

            // Coils must be Bool type
            var badCoilsType = tags.Where(t => t.Register == RegisterType.Coils && t.Type != DataType.Bool).ToList();
            if (badCoilsType.Count > 0)
            {
                errors.AppendLine($"Для Coils поддерживается только тип Bool ({badCoilsType.Count} шт. нарушений).");
            }

            // Overlap detection per register type (consider word size); coils are 1 bit each
            foreach (var regGroup in tags.GroupBy(t => t.Register))
            {
                var intervals = regGroup
                    .Select(t => new { t.Name, Start = (int)t.Address, End = (int)(t.Address + (t.Register == RegisterType.Coils ? 1 : GetWordCount(t.Type)) - 1) })
                    .OrderBy(i => i.Start)
                    .ToList();
                for (int i = 1; i < intervals.Count; i++)
                {
                    if (intervals[i].Start <= intervals[i - 1].End)
                    {
                        errors.AppendLine($"Перекрытие адресов в {regGroup.Key}: '{intervals[i - 1].Name}' и '{intervals[i].Name}'");
                    }
                }
            }

            if (errors.Length > 0)
            {
                // Show simple modal dialog with errors
                var dlg = new Window
                {
                    Width = 480,
                    Height = 260,
                    Title = "Ошибки в карте тегов",
                    Content = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Children =
                        {
                            new TextBlock{ Text="Исправьте ошибки и попробуйте снова:", FontWeight = Avalonia.Media.FontWeight.Bold, Margin=new Thickness(0,0,0,8)},
                            new ScrollViewer{ Content = new TextBlock{ Text = errors.ToString(), TextWrapping = Avalonia.Media.TextWrapping.Wrap } },
                            new StackPanel{ Orientation= Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin=new Thickness(0,12,0,0), Children={ new Button{ Content="OK", Width=80 } } }
                        }
                    }
                };
                if (dlg.Content is StackPanel sp && sp.Children.LastOrDefault() is StackPanel btnRow && btnRow.Children.FirstOrDefault() is Button okBtn)
                {
                    okBtn.Click += (_, __) => dlg.Close();
                }
                dlg.ShowDialog(this);
                return;
            }
        }
        Close(true);
    }

    private static int GetWordCount(DataType type) => type switch
    {
        DataType.UInt16 or DataType.Int16 or DataType.Bool => 1,
        DataType.UInt32 or DataType.Int32 or DataType.Float32 => 2,
        DataType.Int64 or DataType.Double => 4,
        _ => 1
    };

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void AddTag_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel vm)
        {
            vm.ConnectionConfig.Tags.Add(new TagDefinition
            {
                Enabled = true,
                Name = "NewTag",
                Address = 0,
                Register = RegisterType.Holding,
                Type = DataType.UInt16,
                WordOrder = WordOrder.HighLow,
                Scale = 1.0,
                Offset = 0.0
            });
        }
    }

    private void RemoveSelectedTag_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel vm)
        {
            if (this.FindControl<DataGrid>("TagsGrid") is { } grid && grid.SelectedItem is TagDefinition tag)
            {
                vm.ConnectionConfig.Tags.Remove(tag);
            }
            else if (vm.ConnectionConfig.Tags.Count > 0)
            {
                vm.ConnectionConfig.Tags.RemoveAt(vm.ConnectionConfig.Tags.Count - 1);
            }
        }
    }
}
