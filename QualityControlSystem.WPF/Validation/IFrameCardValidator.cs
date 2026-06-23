using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Validation;

public interface IFrameCardValidator
{
    ValidationResult Validate(FrameCardDto frame);
}
