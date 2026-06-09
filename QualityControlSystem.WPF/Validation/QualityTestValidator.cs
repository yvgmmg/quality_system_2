using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Validation;

public sealed class QualityTestValidator : IQualityTestValidator
{
    public ValidationResult Validate(QualityTestDto test)
    {
        if (string.IsNullOrWhiteSpace(test.Name))
            return ValidationResult.Fail("Укажите название теста.");

        if (test.FrameId <= 0)
            return ValidationResult.Fail("Выберите модель каркаса.");

        if (test.TemplateIds.Count == 0)
            return ValidationResult.Fail("Привяжите хотя бы один шаблон.");

        return ValidationResult.Success();
    }
}
