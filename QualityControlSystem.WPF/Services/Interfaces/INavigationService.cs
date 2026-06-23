using System;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
        void NavigateTo(Type viewModelType);
    }
}
