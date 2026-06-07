using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QualityControlSystem.WPF.Services;

public static class TemplatePreviewLoader
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    public static ImageSource? Load(string? imagePath)
    {
        var previewPath = FindPreviewImage(imagePath);
        if (previewPath == null)
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 110;
            image.UriSource = new Uri(previewPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindPreviewImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        try
        {
            var absolutePath = Path.GetFullPath(imagePath);
            if (File.Exists(absolutePath) && IsSupportedImagePath(absolutePath))
                return absolutePath;

            if (!Directory.Exists(absolutePath))
                return null;

            return Directory.EnumerateFiles(absolutePath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedImagePath)
                .OrderByDescending(path => Path.GetFileName(path).StartsWith("origin", StringComparison.OrdinalIgnoreCase))
                .ThenBy(path => Path.GetFileName(path))
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSupportedImagePath(string path)
    {
        return SupportedImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }
}
