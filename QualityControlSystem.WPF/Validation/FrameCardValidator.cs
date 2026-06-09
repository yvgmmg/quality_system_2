using QualityControlSystem.WPF.Dtos;
using System.IO;

namespace QualityControlSystem.WPF.Validation;

public class FrameCardValidator : IFrameCardValidator
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    public ValidationResult Validate(FrameCardDto frame)
    {
        if (string.IsNullOrWhiteSpace(frame.Name))
            return ValidationResult.Fail("Укажите название каркаса.");

        if (frame.MaterialTypeId <= 0)
            return ValidationResult.Fail("Выберите материал.");

        if (frame.WorkshopId <= 0)
            return ValidationResult.Fail("Выберите цех.");

        var imagePath = NormalizeImagePath(frame.ImagePath);
        if (imagePath != null && !IsSupportedImagePath(imagePath))
            return ValidationResult.Fail("Путь к изображению должен указывать на PNG или JPEG файл.");

        return ValidationResult.Success();
    }

    private static string? NormalizeImagePath(string? imagePath)
    {
        return string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    }

    private static bool IsSupportedImagePath(string path)
    {
        return SupportedImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }
}
