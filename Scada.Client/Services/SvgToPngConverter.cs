using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace Scada.Client.Services;

/// <summary>
/// Утилита для конвертации SVG в PNG
/// </summary>
public static class SvgToPngConverter
{
    /// <summary>
    /// Конвертирует SVG файл в PNG
    /// </summary>
    /// <param name="svgPath">Путь к SVG файлу</param>
    /// <param name="width">Ширина результирующего PNG (если null, используется размер из SVG)</param>
    /// <param name="height">Высота результирующего PNG (если null, используется размер из SVG)</param>
    /// <returns>Путь к созданному PNG файлу или null при ошибке</returns>
    public static string? ConvertSvgToPng(string svgPath, int? width = null, int? height = null)
    {
        try
        {
            if (!File.Exists(svgPath))
            {
                System.Diagnostics.Debug.WriteLine($"SVG файл не найден: {svgPath}");
                return null;
            }

            // Читаем и парсим SVG
            var svg = new SKSvg();
            svg.Load(svgPath);

            if (svg.Picture == null)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось загрузить SVG: {svgPath}");
                return null;
            }

            // Определяем размеры
            var svgSize = svg.Picture.CullRect;
            var targetWidth = width ?? (int)svgSize.Width;
            var targetHeight = height ?? (int)svgSize.Height;

            if (targetWidth <= 0 || targetHeight <= 0)
            {
                targetWidth = 50; // Размер по умолчанию
                targetHeight = 50;
            }

            // Создаём PNG
            var imageInfo = new SKImageInfo(targetWidth, targetHeight);
            using var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            // Масштабируем SVG
            var scaleX = targetWidth / svgSize.Width;
            var scaleY = targetHeight / svgSize.Height;
            var matrix = SKMatrix.CreateScale(scaleX, scaleY);
            
            canvas.DrawPicture(svg.Picture, ref matrix);
            canvas.Flush();

            // Сохраняем PNG в ту же папку, что и SVG
            var pngPath = Path.ChangeExtension(svgPath, ".png");
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var pngStream = File.OpenWrite(pngPath);
            data.SaveTo(pngStream);

            System.Diagnostics.Debug.WriteLine($"SVG конвертирован в PNG: {svgPath} -> {pngPath}");
            return pngPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка конвертации SVG в PNG '{svgPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Проверяет, является ли файл SVG
    /// </summary>
    public static bool IsSvgFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var extension = Path.GetExtension(path);
        return extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }
}
