using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "Информация");
        bool ShowConfirm(string message, string title = "Подтверждение");
    }
}
