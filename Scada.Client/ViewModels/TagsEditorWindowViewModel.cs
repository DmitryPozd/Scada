using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using Scada.Client.Models;
using Scada.Client.Services;

namespace Scada.Client.ViewModels;

public class TagsEditorWindowViewModel : ViewModelBase
{
    private readonly ITagsConfigService _tagsConfigService;
    private TagsConfiguration? _originalConfiguration;
    
    private ObservableCollection<TagDefinition> _allTags = new();
    private ObservableCollection<TagDefinition> _filteredTags = new();
    private TagDefinition? _selectedTag;
    private string _searchText = string.Empty;
    private ComboBoxItem? _selectedFilterType;
    private string _tagsFilePath = string.Empty;
    private string _tagsCountInfo = string.Empty;

    public TagsEditorWindowViewModel()
    {
        _tagsConfigService = new TagsConfigService();
        
        AddTagCommand = ReactiveCommand.Create(AddNewTag);
        RemoveTagCommand = ReactiveCommand.Create(RemoveSelectedTag, 
            this.WhenAnyValue(x => x.SelectedTag).Select(tag => tag != null));
        SaveCommand = ReactiveCommand.CreateFromTask(SaveTagsAsync);

        // Подписка на изменения фильтров
        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => ApplyFilters());
        this.WhenAnyValue(x => x.SelectedFilterType)
            .Subscribe(_ => ApplyFilters());
    }

    public ObservableCollection<TagDefinition> AllTags
    {
        get => _allTags;
        set => this.RaiseAndSetIfChanged(ref _allTags, value);
    }

    public ObservableCollection<TagDefinition> FilteredTags
    {
        get => _filteredTags;
        set => this.RaiseAndSetIfChanged(ref _filteredTags, value);
    }

    public TagDefinition? SelectedTag
    {
        get => _selectedTag;
        set => this.RaiseAndSetIfChanged(ref _selectedTag, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ComboBoxItem? SelectedFilterType
    {
        get => _selectedFilterType;
        set => this.RaiseAndSetIfChanged(ref _selectedFilterType, value);
    }

    public string TagsFilePath
    {
        get => _tagsFilePath;
        set => this.RaiseAndSetIfChanged(ref _tagsFilePath, value);
    }

    public string TagsCountInfo
    {
        get => _tagsCountInfo;
        set => this.RaiseAndSetIfChanged(ref _tagsCountInfo, value);
    }

    public ReactiveCommand<Unit, Unit> AddTagCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveTagCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    /// <summary>
    /// Загрузить теги из файла
    /// </summary>
    public async Task LoadTagsAsync()
    {
        var config = await _tagsConfigService.LoadTagsConfigurationAsync();
        _originalConfiguration = config;

        if (config != null && config.Tags.Count > 0)
        {
            AllTags = new ObservableCollection<TagDefinition>(config.Tags);
        }
        else
        {
            AllTags = new ObservableCollection<TagDefinition>();
        }

        // Определяем путь к файлу
        var service = _tagsConfigService as TagsConfigService;
        if (service != null)
        {
            var appDataPath = service.GetAppDataTagsFilePath();
            var appDirPath = service.GetAppDirectoryTagsFilePath();
            
            if (File.Exists(appDataPath))
            {
                TagsFilePath = appDataPath;
            }
            else if (File.Exists(appDirPath))
            {
                TagsFilePath = appDirPath;
            }
            else
            {
                TagsFilePath = appDataPath; // Будет создан при сохранении
            }
        }

        ApplyFilters();
    }

    /// <summary>
    /// Применить фильтры к списку тегов
    /// </summary>
    private void ApplyFilters()
    {
        var filtered = AllTags.AsEnumerable();

        // Фильтр по типу регистра
        if (SelectedFilterType?.Tag is string filterTag && filterTag != "All")
        {
            if (Enum.TryParse<RegisterType>(filterTag, out var registerType))
            {
                filtered = filtered.Where(t => t.Register == registerType);
            }
        }

        // Фильтр по тексту поиска
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Name.ToLowerInvariant().Contains(searchLower) ||
                t.Address.ToString().Contains(searchLower));
        }

        var filteredList = filtered.ToList();
        FilteredTags = new ObservableCollection<TagDefinition>(filteredList);
        
        TagsCountInfo = $"Показано: {filteredList.Count} / Всего: {AllTags.Count}";
    }

    /// <summary>
    /// Добавить новый тег
    /// </summary>
    private void AddNewTag()
    {
        var newTag = new TagDefinition
        {
            Name = $"NewTag_{AllTags.Count + 1}",
            Address = 0,
            Register = RegisterType.Coils,
            Type = DataType.Bool,
            Enabled = true,
            Scale = 1.0,
            Offset = 0.0,
            WordOrder = WordOrder.HighLow
        };

        AllTags.Add(newTag);
        ApplyFilters();
        SelectedTag = newTag;
    }

    /// <summary>
    /// Удалить выбранный тег
    /// </summary>
    private void RemoveSelectedTag()
    {
        if (SelectedTag != null)
        {
            AllTags.Remove(SelectedTag);
            ApplyFilters();
        }
    }

    /// <summary>
    /// Сохранить теги в файл
    /// </summary>
    private async Task SaveTagsAsync()
    {
        try
        {
            // Создаём новую конфигурацию
            var config = new TagsConfiguration
            {
                Tags = AllTags.OrderBy(t => t.Address).ToList(),
                Groups = _originalConfiguration?.Groups ?? new TagGroups()
            };

            // Определяем путь для сохранения (приоритет AppData)
            var service = _tagsConfigService as TagsConfigService;
            var savePath = service?.GetAppDataTagsFilePath() ?? TagsFilePath;

            // Создаём каталог если не существует
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Сериализуем и сохраняем
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(savePath, json);

            System.Diagnostics.Debug.WriteLine($"Tags saved successfully to: {savePath}");
            
            // Можно показать сообщение об успехе
            // TODO: добавить уведомление пользователю
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving tags: {ex.Message}");
            // TODO: показать окно с ошибкой
        }
    }

    /// <summary>
    /// Для ComboBox - список типов регистров
    /// </summary>
    public RegisterType[] RegisterTypes => Enum.GetValues<RegisterType>();
    
    /// <summary>
    /// Для ComboBox - список типов данных
    /// </summary>
    public DataType[] DataTypes => Enum.GetValues<DataType>();
    
    /// <summary>
    /// Для ComboBox - список порядков слов
    /// </summary>
    public WordOrder[] WordOrders => Enum.GetValues<WordOrder>();
}
