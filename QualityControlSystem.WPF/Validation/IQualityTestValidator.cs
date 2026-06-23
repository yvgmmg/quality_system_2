using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Validation;

public interface IQualityTestValidator
{
    ValidationResult Validate(QualityTestDto test);
}
