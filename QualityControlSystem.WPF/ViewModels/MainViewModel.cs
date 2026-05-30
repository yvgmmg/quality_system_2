using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using QualityControlSystem.WPF.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private UserControl? _currentView;

        [ObservableProperty]
        private string _currentUserName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MenuItemViewModel> _menuItems = new();

        [ObservableProperty]
        private GridLength _navigationColumnWidth = new(0);

        [ObservableProperty]
        private Visibility _navigationVisibility = Visibility.Collapsed;

        public MainViewModel(INavigationService navigationService, IAuthService authService)
        {
            _navigationService = navigationService;
            _authService = authService;

            _authService.CurrentUserChanged += OnCurrentUserChanged;
            UpdateUserState();
        }

        private void OnCurrentUserChanged(UserProfileDto? user)
        {
            UpdateUserState();
        }

        private void UpdateUserState()
        {
            if (_authService.CurrentUser != null)
            {
                CurrentUserName = $"{_authService.CurrentUser.Surname} {_authService.CurrentUser.Name} {_authService.CurrentUser.Patron}".Trim();
                NavigationColumnWidth = new GridLength(220);
                NavigationVisibility = Visibility.Visible;
                BuildMenu();
            }
            else
            {
                CurrentUserName = string.Empty;
                NavigationColumnWidth = new GridLength(0);
                NavigationVisibility = Visibility.Collapsed;
                MenuItems.Clear();
            }
        }

        private void BuildMenu()
        {
            MenuItems.Clear();
            MenuItems.Add(new MenuItemViewModel { Header = "Профиль", ViewType = typeof(ProfileView) });

            if (_authService.CurrentUser?.Role?.ToLower() == "admin")
            {
                MenuItems.Add(new MenuItemViewModel { Header = "Управление пользователями", ViewType = typeof(UserManagementView) });
            }

            MenuItems.Add(new MenuItemViewModel { Header = "Выйти", IsExit = true });
        }

        [RelayCommand]
        private void Navigate(MenuItemViewModel menuItem)
        {
            if (menuItem.IsExit)
            {
                _authService.Logout();
                _navigationService.NavigateTo<LoginView>();
                return;
            }

            if (menuItem.ViewType != null)
                _navigationService.NavigateTo(menuItem.ViewType);
        }
    }

    public class MenuItemViewModel
    {
        public string Header { get; set; } = string.Empty;
        public System.Type? ViewType { get; set; }
        public bool IsExit { get; set; }
    }
}
