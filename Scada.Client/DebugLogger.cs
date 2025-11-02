using System;
using System.IO;

namespace Scada.Client;

public static class DebugLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Scada.Client",
        "debug.log"
    );

    static DebugLogger()
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir!);
        }
        
        // Очищаем файл при запуске
        File.WriteAllText(LogPath, $"=== START {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
    }

    public static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
            File.AppendAllText(LogPath, line);
            Console.WriteLine(line); // Дублируем в консоль
        }
        catch
        {
            // Игнорируем ошибки логирования
        }
    }

    public static string GetLogPath() => LogPath;
}
