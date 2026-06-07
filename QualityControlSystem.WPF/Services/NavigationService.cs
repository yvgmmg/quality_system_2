using System;
using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Services.Navigation;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly NavigationStore _navigationStore;

        public NavigationService(IServiceScopeFactory scopeFactory, NavigationStore navigationStore)
        {
            _scopeFactory = scopeFactory;
            _navigationStore = navigationStore;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var scope = _scopeFactory.CreateScope();
            var viewModel = scope.ServiceProvider.GetRequiredService<TViewModel>();
            _navigationStore.SetCurrentViewModel(viewModel, scope);
        }

        public void NavigateTo(Type viewModelType)
        {
            if (!typeof(BaseViewModel).IsAssignableFrom(viewModelType))
            {
                throw new ArgumentException(
                    $"Type '{viewModelType.FullName}' must inherit from {nameof(BaseViewModel)}.",
                    nameof(viewModelType));
            }

            var scope = _scopeFactory.CreateScope();
            var viewModel = (BaseViewModel)scope.ServiceProvider.GetRequiredService(viewModelType);
            _navigationStore.SetCurrentViewModel(viewModel, scope);
        }
    }
}
