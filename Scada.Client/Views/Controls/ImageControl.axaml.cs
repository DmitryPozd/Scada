using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Scada.Client.Views.Controls;

public partial class ImageControl : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ImageControl, string>(nameof(Label), defaultValue: string.Empty);

    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<ImageControl, bool>(nameof(ShowLabel), defaultValue: true);

    public static readonly StyledProperty<string> ImagePathProperty =
        AvaloniaProperty.Register<ImageControl, string>(nameof(ImagePath), defaultValue: string.Empty);

    public static readonly StyledProperty<double> ImageWidthProperty =
        AvaloniaProperty.Register<ImageControl, double>(nameof(ImageWidth), defaultValue: 100.0);

    public static readonly StyledProperty<double> ImageHeightProperty =
        AvaloniaProperty.Register<ImageControl, double>(nameof(ImageHeight), defaultValue: 100.0);

    // События для удаления
    public static readonly RoutedEvent<RoutedEventArgs> DeleteRequestedEvent =
        RoutedEvent.Register<ImageControl, RoutedEventArgs>(nameof(DeleteRequested), RoutingStrategies.Bubble);

    public event EventHandler? ImageChanged;
    public new event EventHandler? SizeChanged;

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool ShowLabel
    {
        get => GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    public string ImagePath
    {
        get => GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public double ImageWidth
    {
        get => GetValue(ImageWidthProperty);
        set => SetValue(ImageWidthProperty, value);
    }

    public double ImageHeight
    {
        get => GetValue(ImageHeightProperty);
        set => SetValue(ImageHeightProperty, value);
    }

    public event EventHandler<RoutedEventArgs> DeleteRequested
    {
        add => AddHandler(DeleteRequestedEvent, value);
        remove => RemoveHandler(DeleteRequestedEvent, value);
    }

    public ImageControl()
    {
        InitializeComponent();
        Focusable = true;

        // Подписка на изменение пути к изображению
        ImagePathProperty.Changed.AddClassHandler<ImageControl>((control, args) =>
        {
            if (args.NewValue is string path && !string.IsNullOrEmpty(path))
            {
                control.LoadImage(path);
            }
        });

        // Установить размер контрола равным размеру изображения
        ImageWidthProperty.Changed.AddClassHandler<ImageControl>((control, args) =>
        {
            if (args.NewValue is double width)
            {
                control.Width = width;
            }
        });

        ImageHeightProperty.Changed.AddClassHandler<ImageControl>((control, args) =>
        {
            if (args.NewValue is double height)
            {
                control.Height = height;
            }
        });
    }

    private void LoadImage(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var image = this.FindControl<Image>("ImageElement");
                if (image != null)
                {
                    image.Source = new Bitmap(path);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Правый клик - показать контекстное меню
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            _ = ShowContextMenuAsync();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Delete - удалить элемент
        if (e.Key == Key.Delete)
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent));
            e.Handled = true;
        }
    }

    private async Task ShowContextMenuAsync()
    {
        var dialog = new Window
        {
            Title = "Настройки изображения",
            Width = 600,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

        // Поле для редактирования названия
        var labelTextBlock = new TextBlock { Text = "Название (подпись):", FontWeight = FontWeight.SemiBold };
        var labelInput = new TextBox
        {
            Text = Label,
            Watermark = "Введите название"
        };
        stack.Children.Add(labelTextBlock);
        stack.Children.Add(labelInput);

        // Показывать ли подпись
        var showLabelCheck = new CheckBox
        {
            Content = "Показывать подпись",
            IsChecked = ShowLabel
        };
        stack.Children.Add(showLabelCheck);

        // Разделитель
        stack.Children.Add(new Separator());

        // Текущий путь к изображению
        var pathText = new TextBlock
        {
            Text = $"Текущее изображение:\n{(string.IsNullOrEmpty(ImagePath) ? "не выбрано" : Path.GetFileName(ImagePath))}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray
        };
        stack.Children.Add(pathText);

        // Кнопка выбора изображения
        var selectImageBtn = new Button
        {
            Content = "📁 Выбрать изображение",
            Padding = new Thickness(10, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        string? selectedPath = ImagePath;
        selectImageBtn.Click += async (s, e) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите изображение",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    selectedPath = files[0].Path.LocalPath;
                    pathText.Text = $"Текущее изображение:\n{Path.GetFileName(selectedPath)}";
                }
            }
        };
        stack.Children.Add(selectImageBtn);

        // Разделитель
        stack.Children.Add(new Separator());

        // Размеры изображения
        var sizeText = new TextBlock { Text = "Размеры изображения:", FontWeight = FontWeight.SemiBold };
        stack.Children.Add(sizeText);

        var sizePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        sizePanel.Children.Add(new TextBlock { Text = "Ширина:", VerticalAlignment = VerticalAlignment.Center });
        var widthInput = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 1000,
            Value = (decimal)ImageWidth,
            Width = 150,
            Increment = 10
        };
        sizePanel.Children.Add(widthInput);

        sizePanel.Children.Add(new TextBlock { Text = "Высота:", VerticalAlignment = VerticalAlignment.Center });
        var heightInput = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 1000,
            Value = (decimal)ImageHeight,
            Width = 150,
            Increment = 10
        };
        sizePanel.Children.Add(heightInput);
        stack.Children.Add(sizePanel);

        // Разделитель
        stack.Children.Add(new Separator());

        // Кнопка удаления
        var deleteBtn = new Button
        {
            Content = "🗑️ Удалить элемент",
            Padding = new Thickness(10, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        deleteBtn.Click += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(DeleteRequestedEvent));
            dialog.Close();
        };
        stack.Children.Add(deleteBtn);

        // Разделитель
        stack.Children.Add(new Separator());

        // Кнопки OK/Отмена
        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var okBtn = new Button { Content = "OK", Width = 80 };
        okBtn.Click += (s, e) =>
        {
            Label = labelInput.Text ?? string.Empty;
            ShowLabel = showLabelCheck.IsChecked ?? true;
            
            if (!string.IsNullOrEmpty(selectedPath))
            {
                ImagePath = selectedPath;
                ImageChanged?.Invoke(this, EventArgs.Empty);
            }

            ImageWidth = (double)(widthInput.Value ?? 100);
            ImageHeight = (double)(heightInput.Value ?? 100);
            SizeChanged?.Invoke(this, EventArgs.Empty);

            dialog.Close();
        };

        var cancelBtn = new Button { Content = "Отмена", Width = 80 };
        cancelBtn.Click += (s, e) => dialog.Close();

        buttonsPanel.Children.Add(okBtn);
        buttonsPanel.Children.Add(cancelBtn);
        stack.Children.Add(buttonsPanel);

        scrollViewer.Content = stack;
        dialog.Content = scrollViewer;

        if (this.VisualRoot is Window owner)
        {
            await dialog.ShowDialog(owner);
        }
    }
}
