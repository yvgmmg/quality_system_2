using CommunityToolkit.Mvvm.ComponentModel;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        [ObservableProperty]
        private UserProfileDto? _user;

        public ProfileViewModel(IAuthService authService)
        {
            User = authService.CurrentUser;
        }
    }
}