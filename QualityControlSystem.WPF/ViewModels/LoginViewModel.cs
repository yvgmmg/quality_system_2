using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using QualityControlSystem.WPF.Views;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private string _login = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        [RelayCommand]  // убрали CanExecute = nameof(CanLogin)
        private async Task LoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var success = await _authService.LoginAsync(Login, Password);
                if (success)
                {
                    _navigationService.NavigateTo<ProfileView>();
                }
                else
                {
                    ErrorMessage = "Неверный логин или пароль.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка: {ex.Message}";
                // при необходимости логируйте ex
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Login) && !string.IsNullOrWhiteSpace(Password);
    }
}
