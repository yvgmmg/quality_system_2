using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Services.Navigation;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;
        private readonly NavigationStore _navigationStore;

        [ObservableProperty]
        private BaseViewModel? _currentViewModel;

        [ObservableProperty]
        private string _currentUserName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MenuItemViewModel> _menuItems = new();

        [ObservableProperty]
        private double _navigationWidth;

        [ObservableProperty]
        private bool _isNavigationVisible;

        public MainViewModel(
            INavigationService navigationService,
            IAuthService authService,
            NavigationStore navigationStore)
        {
            _navigationService = navigationService;
            _authService = authService;
            _navigationStore = navigationStore;

            _authService.CurrentUserChanged += OnCurrentUserChanged;
            _navigationStore.CurrentViewModelChanged += HandleCurrentViewModelChanged;
            UpdateUserState();
        }

        private void HandleCurrentViewModelChanged(BaseViewModel? viewModel)
        {
            CurrentViewModel = viewModel;
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
                NavigationWidth = 220;
                IsNavigationVisible = true;
                BuildMenu();
            }
            else
            {
                CurrentUserName = string.Empty;
                NavigationWidth = 0;
                IsNavigationVisible = false;
                MenuItems.Clear();
            }
        }

        private void BuildMenu()
        {
            MenuItems.Clear();
            MenuItems.Add(new MenuItemViewModel { Header = "Профиль", ViewModelType = typeof(ProfileViewModel) });

            if (HasRoleCode(RoleCodes.Admin))
            {
                MenuItems.Add(new MenuItemViewModel { Header = "Управление пользователями", ViewModelType = typeof(UserManagementViewModel) });
            }

            if (HasRoleCode(RoleCodes.Operator))
            {
                MenuItems.Add(new MenuItemViewModel { Header = "Контроль деталей", ViewModelType = typeof(OperatorControlViewModel) });
                MenuItems.Add(new MenuItemViewModel { Header = "Шаблоны", ViewModelType = typeof(TemplatesViewModel) });
            }

            if (HasRoleCode(RoleCodes.EquipmentSpecialist))
            {
                MenuItems.Add(new MenuItemViewModel { Header = "Оборудование", ViewModelType = typeof(EquipmentManagementViewModel) });
            }

            if (HasRoleCode(RoleCodes.QualityControl))
            {
                MenuItems.Add(new MenuItemViewModel { Header = "Карточки каркасов", ViewModelType = typeof(FrameCardsViewModel) });
                MenuItems.Add(new MenuItemViewModel { Header = "Тесты контроля", ViewModelType = typeof(QualityTestsViewModel) });
            }

            MenuItems.Add(new MenuItemViewModel { Header = "Выйти", IsExit = true });
        }

        private bool HasRoleCode(string roleCode)
        {
            return string.Equals(_authService.CurrentUser?.RoleCode, roleCode, System.StringComparison.OrdinalIgnoreCase);
        }

        private static class RoleCodes
        {
            public const string Admin = "01000001";
            public const string Operator = "02000001";
            public const string EquipmentSpecialist = "03000001";
            public const string QualityControl = "04000001";
        }

        [RelayCommand]
        private void Navigate(MenuItemViewModel menuItem)
        {
            if (menuItem.IsExit)
            {
                _authService.Logout();
                _navigationService.NavigateTo<LoginViewModel>();
                return;
            }

            if (menuItem.ViewModelType != null)
            {
                _navigationService.NavigateTo(menuItem.ViewModelType);
            }
        }
    }

    public class MenuItemViewModel
    {
        public string Header { get; set; } = string.Empty;
        public System.Type? ViewModelType { get; set; }
        public bool IsExit { get; set; }
    }
}
