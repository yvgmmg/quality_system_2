using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Validation;

public interface IEquipmentValidator
{
    ValidationResult Validate(ProductionEquipmentDto equipment);
}
