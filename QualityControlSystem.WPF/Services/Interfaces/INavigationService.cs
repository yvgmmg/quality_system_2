using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface INavigationService
    {
        void Initialize(MainWindow mainWindow);
        void NavigateTo<TView>() where TView : class;
        void GoBack();
    }
}