using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Scada.Client.Models;

namespace Scada.Client.Services;

public class SettingsService : ISettingsService
{
    private const string ProductFolder = "Scada.Client";
    private const string FileName = "settings.json";

    private static string GetFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, ProductFolder);
        return folder;
    }

    private static string GetFilePath()
    {
        var folder = GetFolderPath();
        var path = Path.Combine(folder, FileName);
        System.Diagnostics.Debug.WriteLine($"SettingsService: File path = {path}");
        return path;
    }

    public async Task<ModbusConnectionConfig?> LoadAsync()
    {
        try
        {
            var path = GetFilePath();
            System.Diagnostics.Debug.WriteLine($"SettingsService.LoadAsync: Loading from {path}");
            
            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"SettingsService.LoadAsync: File does not exist");
                return null;
            }

            await using var stream = File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync<ModbusConnectionConfig>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            System.Diagnostics.Debug.WriteLine($"SettingsService.LoadAsync: Loaded {config?.Tags.Count ?? 0} tags");
            return config;
        }
        catch (Exception ex)
        {
            // Ignore corrupted JSON; return null to use defaults
            System.Diagnostics.Debug.WriteLine($"SettingsService.LoadAsync: Error - {ex.Message}");
            return null;
        }
    }

    public async Task SaveAsync(ModbusConnectionConfig config)
    {
        var folder = GetFolderPath();
        Directory.CreateDirectory(folder);
        var path = GetFilePath();

        System.Diagnostics.Debug.WriteLine($"SettingsService.SaveAsync: Saving {config.Tags.Count} tags to {path}");
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(path, json);
        
        System.Diagnostics.Debug.WriteLine($"SettingsService.SaveAsync: File saved successfully");
    }
}
