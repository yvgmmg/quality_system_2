using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private string _welcomeMessage = string.Empty;

        public DashboardViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            if (_authService.CurrentUser != null)
            {
                WelcomeMessage = $"Добро пожаловать, {_authService.CurrentUser.Name} {_authService.CurrentUser.Surname}! Роль: {_authService.CurrentUser.Role}";
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            _authService.Logout();
            _navigationService.NavigateTo<LoginViewModel>();
            await Task.CompletedTask;
        }
    }
}
