using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Scada.Client.Models;
using Scada.Client.Services;

namespace Scada.Client.ViewModels;

public class TagsEditorWindowViewModel : ViewModelBase
{
    private readonly ITagsConfigService _tagsConfigService;
    private ObservableCollection<TagDefinition> _activeTags;
    private ObservableCollection<TagDefinition> _availableTags;
    private TagDefinition? _selectedActiveTag;
    private TagDefinition? _selectedAvailableTag;
    private string _searchText = string.Empty;
    private string _rangeText = string.Empty;
    private System.Collections.IList? _selectedActiveTags;

    public TagsEditorWindowViewModel(ObservableCollection<TagDefinition> activeTags, ITagsConfigService tagsConfigService)
    {
        _tagsConfigService = tagsConfigService;
        _activeTags = new ObservableCollection<TagDefinition>(activeTags);
        _availableTags = new ObservableCollection<TagDefinition>();
        
        AddTagCommand = ReactiveCommand.CreateFromTask(
            AddTagFromAvailable,
            this.WhenAnyValue(vm => vm.SelectedAvailableTag).Select(tag => tag != null)
        );
        DeleteTagCommand = ReactiveCommand.Create(
            DeleteSelectedTags, 
            this.WhenAnyValue(vm => vm.SelectedActiveTags).Select(items => items != null && items.Count > 0)
        );
        DeleteAllTagsCommand = ReactiveCommand.Create(DeleteAllTags);
        SearchTagsCommand = ReactiveCommand.CreateFromTask(SearchTagsAsync);
        AddRangeCommand = ReactiveCommand.CreateFromTask(AddTagsByRangeAsync);
        
        // Загрузить доступные тэги при создании
        _ = LoadAvailableTagsAsync();
    }

    public ObservableCollection<TagDefinition> ActiveTags
    {
        get => _activeTags;
        set => this.RaiseAndSetIfChanged(ref _activeTags, value);
    }

    public ObservableCollection<TagDefinition> AvailableTags
    {
        get => _availableTags;
        set => this.RaiseAndSetIfChanged(ref _availableTags, value);
    }

    public TagDefinition? SelectedActiveTag
    {
        get => _selectedActiveTag;
        set => this.RaiseAndSetIfChanged(ref _selectedActiveTag, value);
    }

    public System.Collections.IList? SelectedActiveTags
    {
        get => _selectedActiveTags;
        set => this.RaiseAndSetIfChanged(ref _selectedActiveTags, value);
    }

    public TagDefinition? SelectedAvailableTag
    {
        get => _selectedAvailableTag;
        set => this.RaiseAndSetIfChanged(ref _selectedAvailableTag, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public string RangeText
    {
        get => _rangeText;
        set => this.RaiseAndSetIfChanged(ref _rangeText, value);
    }

    public ReactiveCommand<Unit, Unit> AddTagCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteTagCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteAllTagsCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchTagsCommand { get; }
    public ReactiveCommand<Unit, Unit> AddRangeCommand { get; }

    private async Task LoadAvailableTagsAsync()
    {
        var config = await _tagsConfigService.LoadConfigurationAsync();
        if (config?.Tags != null)
        {
            // Показываем только тэги, которых ещё нет в активных
            var activeNames = new HashSet<string>(ActiveTags.Select(t => t.Name));
            var available = config.Tags.Where(t => !activeNames.Contains(t.Name)).ToList();
            
            AvailableTags.Clear();
            foreach (var tag in available.Take(100)) // Ограничиваем первые 100
            {
                AvailableTags.Add(tag);
            }
        }
    }

    private async Task SearchTagsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadAvailableTagsAsync();
            return;
        }

        var config = await _tagsConfigService.LoadConfigurationAsync();
        if (config?.Tags == null) return;

        var activeNames = new HashSet<string>(ActiveTags.Select(t => t.Name));
        var search = SearchText.ToUpperInvariant();
        
        var filtered = config.Tags
            .Where(t => !activeNames.Contains(t.Name))
            .Where(t => t.Name.ToUpperInvariant().Contains(search) || 
                       t.Address.ToString().Contains(search))
            .Take(100)
            .ToList();

        AvailableTags.Clear();
        foreach (var tag in filtered)
        {
            AvailableTags.Add(tag);
        }
    }

    private async Task AddTagFromAvailable()
    {
        if (SelectedAvailableTag != null)
        {
            // Создаём копию тэга
            var newTag = new TagDefinition
            {
                Name = SelectedAvailableTag.Name,
                Address = SelectedAvailableTag.Address,
                Register = SelectedAvailableTag.Register,
                Type = SelectedAvailableTag.Type,
                Scale = SelectedAvailableTag.Scale,
                Offset = SelectedAvailableTag.Offset,
                Enabled = true // По умолчанию включен
            };
            
            ActiveTags.Add(newTag);
            AvailableTags.Remove(SelectedAvailableTag);
            SelectedActiveTag = newTag;
            
            await Task.CompletedTask;
        }
    }

    private void DeleteSelectedTags()
    {
        if (SelectedActiveTags == null || SelectedActiveTags.Count == 0)
            return;

        // Создаём копию списка для безопасного удаления
        var tagsToRemove = SelectedActiveTags.Cast<TagDefinition>().ToList();
        
        foreach (var tag in tagsToRemove)
        {
            ActiveTags.Remove(tag);
            // Возвращаем в доступные
            AvailableTags.Add(tag);
        }
        
        System.Diagnostics.Debug.WriteLine($"Deleted {tagsToRemove.Count} tags");
    }

    private void DeleteAllTags()
    {
        var count = ActiveTags.Count;
        
        // Переносим все активные тэги обратно в доступные
        var allTags = ActiveTags.ToList();
        foreach (var tag in allTags)
        {
            AvailableTags.Add(tag);
        }
        
        ActiveTags.Clear();
        System.Diagnostics.Debug.WriteLine($"Deleted all {count} tags");
    }

    private async Task AddTagsByRangeAsync()
    {
        if (string.IsNullOrWhiteSpace(RangeText))
            return;

        // Парсим диапазон: "Y0-Y50", "M100-M200", "V512-V600" и т.д.
        var parts = RangeText.Split('-');
        if (parts.Length != 2)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid range format: {RangeText}. Expected format: Y0-Y50");
            return;
        }

        var startName = parts[0].Trim();
        var endName = parts[1].Trim();

        // Извлекаем префикс и числа
        if (!TryParseTagName(startName, out var prefix, out var startNum) ||
            !TryParseTagName(endName, out var endPrefix, out var endNum))
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse tag names: {startName}, {endName}");
            return;
        }

        if (prefix != endPrefix)
        {
            System.Diagnostics.Debug.WriteLine($"Tag prefixes must match: {prefix} != {endPrefix}");
            return;
        }

        if (startNum > endNum)
        {
            System.Diagnostics.Debug.WriteLine($"Start number must be <= end number: {startNum} > {endNum}");
            return;
        }

        // Загружаем все тэги из конфигурации
        var config = await _tagsConfigService.LoadConfigurationAsync();
        if (config?.Tags == null)
        {
            System.Diagnostics.Debug.WriteLine("No tags configuration loaded");
            return;
        }

        var activeNames = new HashSet<string>(ActiveTags.Select(t => t.Name));
        var addedCount = 0;

        // Добавляем тэги в диапазоне
        for (int i = startNum; i <= endNum; i++)
        {
            var tagName = $"{prefix}{i}";
            if (activeNames.Contains(tagName))
                continue; // Уже добавлен

            var tag = config.Tags.FirstOrDefault(t => t.Name == tagName);
            if (tag != null)
            {
                var newTag = new TagDefinition
                {
                    Name = tag.Name,
                    Address = tag.Address,
                    Register = tag.Register,
                    Type = tag.Type,
                    Scale = tag.Scale,
                    Offset = tag.Offset,
                    Enabled = true
                };
                ActiveTags.Add(newTag);
                activeNames.Add(tagName);
                addedCount++;
            }
        }

        System.Diagnostics.Debug.WriteLine($"Added {addedCount} tags from range {RangeText}");
        RangeText = string.Empty; // Очищаем поле

        // Обновляем список доступных (убираем добавленные)
        await LoadAvailableTagsAsync();
    }

    private bool TryParseTagName(string tagName, out string prefix, out int number)
    {
        prefix = string.Empty;
        number = 0;

        if (string.IsNullOrEmpty(tagName))
            return false;

        // Находим первую цифру
        int digitIndex = -1;
        for (int i = 0; i < tagName.Length; i++)
        {
            if (char.IsDigit(tagName[i]))
            {
                digitIndex = i;
                break;
            }
        }

        if (digitIndex <= 0)
            return false;

        prefix = tagName.Substring(0, digitIndex);
        var numStr = tagName.Substring(digitIndex);

        return int.TryParse(numStr, out number);
    }
}
