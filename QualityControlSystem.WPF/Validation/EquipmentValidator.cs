using QualityControlSystem.WPF.Constants;
using QualityControlSystem.WPF.Dtos;

namespace QualityControlSystem.WPF.Validation;

public class EquipmentValidator : IEquipmentValidator
{
    public ValidationResult Validate(ProductionEquipmentDto equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.Name))
            return ValidationResult.Fail("Укажите название оборудования.");

        if (string.IsNullOrWhiteSpace(equipment.OkofCode))
            return ValidationResult.Fail("Укажите код ОКОФ.");

        if (string.IsNullOrWhiteSpace(equipment.InventoryNumber))
            return ValidationResult.Fail("Укажите инвентарный номер.");

        if (equipment.WorkshopId <= 0)
            return ValidationResult.Fail("Выберите цех.");

        if (equipment.Name.Trim().Length > EquipmentValidationRules.NameMaxLength)
            return ValidationResult.Fail($"Название оборудования не должно быть длиннее {EquipmentValidationRules.NameMaxLength} символов.");

        if (NormalizeOptionalText(equipment.SerialNumber)?.Length > EquipmentValidationRules.SerialNumberMaxLength)
            return ValidationResult.Fail($"Серийный номер не должен быть длиннее {EquipmentValidationRules.SerialNumberMaxLength} символов.");

        if (equipment.OkofCode.Trim().Length > EquipmentValidationRules.OkofMaxLength)
            return ValidationResult.Fail($"Код ОКОФ не должен быть длиннее {EquipmentValidationRules.OkofMaxLength} символов.");

        if (equipment.InventoryNumber.Trim().Length > EquipmentValidationRules.InventoryNumberMaxLength)
            return ValidationResult.Fail($"Инвентарный номер не должен быть длиннее {EquipmentValidationRules.InventoryNumberMaxLength} символов.");

        return ValidationResult.Success();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
