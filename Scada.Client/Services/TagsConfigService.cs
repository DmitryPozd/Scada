using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Scada.Client.Models;

namespace Scada.Client.Services;

public interface ITagsConfigService
{
    Task<List<TagDefinition>> LoadTagsAsync();
    Task<List<TagDefinition>> LoadFirstNTagsPerTypeAsync(int count = 20);
    Task<TagsConfiguration?> LoadConfigurationAsync();
}

public class TagsConfigService : ITagsConfigService
{
    private readonly string _tagsFilePath;

    public TagsConfigService()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _tagsFilePath = Path.Combine(baseDir, "tags.json");
    }

    public async Task<List<TagDefinition>> LoadTagsAsync()
    {
        if (!File.Exists(_tagsFilePath))
        {
            Console.WriteLine($"Tags file not found: {_tagsFilePath}");
            return new List<TagDefinition>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_tagsFilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            
            var config = JsonSerializer.Deserialize<TagsConfiguration>(json, options);
            
            if (config?.Tags != null)
            {
                Console.WriteLine($"Loaded {config.Tags.Count} tags from tags.json");
                
                // Выводим первые 5 тегов для проверки
                for (int i = 0; i < Math.Min(5, config.Tags.Count); i++)
                {
                    var tag = config.Tags[i];
                    Console.WriteLine($"  Tag {i}: {tag.Name}, Address={tag.Address}, Register={tag.Register}, Type={tag.Type}");
                }
                
                return config.Tags;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tags.json: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return new List<TagDefinition>();
    }

    public async Task<TagsConfiguration?> LoadConfigurationAsync()
    {
        if (!File.Exists(_tagsFilePath))
        {
            Console.WriteLine($"Tags file not found: {_tagsFilePath}");
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_tagsFilePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            
            return JsonSerializer.Deserialize<TagsConfiguration>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tags.json: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TagDefinition>> LoadFirstNTagsPerTypeAsync(int count = 20)
    {
        var allTags = await LoadTagsAsync();
        if (allTags.Count == 0)
            return new List<TagDefinition>();

        var result = new List<TagDefinition>();

        // Группируем теги по префиксу имени (X, Y, M, T, C, SM, S, AI, AQ, V, TV, CV, SV)
        var tagGroups = allTags.GroupBy(t => GetTagPrefix(t.Name));

        foreach (var group in tagGroups)
        {
            var firstN = group.Take(count).ToList();
            result.AddRange(firstN);
            Console.WriteLine($"  {group.Key}: {firstN.Count} tags (из {group.Count()})");
        }

        Console.WriteLine($"Total tags loaded into settings: {result.Count}");
        return result;
    }

    private string GetTagPrefix(string tagName)
    {
        // Извлекаем префикс из имени тега (X, Y, M, T, C, SM, S, AI, AQ, V, TV, CV, SV)
        if (string.IsNullOrEmpty(tagName))
            return "Unknown";

        // Двухбуквенные префиксы
        if (tagName.Length >= 2)
        {
            var twoChar = tagName.Substring(0, 2);
            if (twoChar == "SM" || twoChar == "AI" || twoChar == "AQ" || 
                twoChar == "TV" || twoChar == "CV" || twoChar == "SV")
                return twoChar;
        }

        // Однобуквенные префиксы
        if (tagName.Length >= 1)
        {
            var oneChar = tagName.Substring(0, 1);
            if (oneChar == "X" || oneChar == "Y" || oneChar == "M" || 
                oneChar == "T" || oneChar == "C" || oneChar == "S" || oneChar == "V")
                return oneChar;
        }

        return "Unknown";
    }
}
