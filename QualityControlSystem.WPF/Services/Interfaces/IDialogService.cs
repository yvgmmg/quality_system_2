using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QualityControlSystem.WPF.Dtos;
namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "Информация");
        bool ShowConfirm(string message, string title = "Подтверждение");
        bool ShowUserDialog(UserProfileDto user, bool isEdit);
        bool ShowProductionEquipmentDialog(ProductionEquipmentDto equipment, IEnumerable<LookupItemDto> workshops, bool isEdit);
        bool ShowFrameDialog(FrameCardDto frame, IEnumerable<LookupItemDto> materials, IEnumerable<LookupItemDto> workshops, bool isEdit);
        bool ShowTemplateDialog(TemplateDto template, IEnumerable<string> sides, bool isEdit);
        bool ShowQualityTestDialog(
            QualityTestDto test,
            IEnumerable<LookupItemDto> frames,
            IEnumerable<TemplateDto> templates,
            bool isEdit);
    }
}
