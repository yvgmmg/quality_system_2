using QualityControlSystem.WPF.Views;
using System;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface INavigationService
    {
        void Initialize(MainWindow mainWindow);
        void NavigateTo<TView>() where TView : UserControl;
        void NavigateTo(Type viewType);
        void GoBack();
        event Action<UserControl>? CurrentViewChanged;
    }
}