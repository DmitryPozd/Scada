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
        return Path.Combine(folder, FileName);
    }

    public async Task<ModbusConnectionConfig?> LoadAsync()
    {
        try
        {
            var path = GetFilePath();
            if (!File.Exists(path))
                return null;

            await using var stream = File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync<ModbusConnectionConfig>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config;
        }
        catch
        {
            // Ignore corrupted JSON; return null to use defaults
            return null;
        }
    }

    public async Task SaveAsync(ModbusConnectionConfig config)
    {
        var folder = GetFolderPath();
        Directory.CreateDirectory(folder);
        var path = GetFilePath();

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(path, json);
    }
}
