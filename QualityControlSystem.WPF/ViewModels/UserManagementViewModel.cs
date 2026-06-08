using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private const string AllRolesFilter = "Все роли";
        private const string AllWorkshopsFilter = "Все цеха";

        private readonly IUserManagementService _userService;
        private readonly IDialogService _dialogService;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private ObservableCollection<UserProfileDto> _users = new();

        [ObservableProperty]
        private ICollectionView? _usersView;

        [ObservableProperty]
        private UserProfileDto? _selectedUser;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _roleFilterOptions = new();

        [ObservableProperty]
        private string _selectedRoleFilter = AllRolesFilter;

        [ObservableProperty]
        private ObservableCollection<string> _workshopFilterOptions = new();

        [ObservableProperty]
        private string _selectedWorkshopFilter = AllWorkshopsFilter;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private ObservableCollection<LookupItemDto> _workshopOptions = new();

        public UserManagementViewModel(
            IUserManagementService userService,
            IDialogService dialogService,
            IAuthService authService)
        {
            _userService = userService;
            _dialogService = dialogService;
            _authService = authService;
            UsersView = CollectionViewSource.GetDefaultView(Users);
            UsersView.Filter = FilterUser;
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            IsBusy = true;
            StatusMessage = "Загрузка пользователей...";

            try
            {
                var users = await _userService.GetAllUsersAsync();
                var workshops = await _userService.GetWorkshopOptionsAsync();

                WorkshopOptions.Clear();
                foreach (var workshop in workshops)
                    WorkshopOptions.Add(workshop);

                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);

                RebuildFilterOptions();
                UsersView?.Refresh();
                StatusMessage = Users.Count == 0
                    ? "Пользователи не найдены."
                    : $"Пользователей: {Users.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка загрузки пользователей: {GetErrorMessage(ex)}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AddUserAsync()
        {
            var newUser = new UserProfileDto { Role = "operator" };
            if (!_dialogService.ShowUserDialog(newUser, WorkshopOptions, false))
                return;

            try
            {
                await _userService.AddUserAsync(newUser);
                await LoadUsersAsync();
                StatusMessage = string.IsNullOrWhiteSpace(newUser.Password)
                    ? "Пользователь добавлен. Пароль по умолчанию: default123"
                    : "Пользователь добавлен с указанным паролем.";
            }
            catch (Exception ex)
            {
                var message = $"Не удалось добавить пользователя: {GetErrorMessage(ex)}";
                StatusMessage = message;
                _dialogService.ShowMessage(message, "Ошибка");
            }
        }

        [RelayCommand]
        private async Task EditUserAsync()
        {
            if (SelectedUser == null)
            {
                StatusMessage = "Выберите пользователя.";
                return;
            }

            var editDto = new UserProfileDto
            {
                Id = SelectedUser.Id,
                Name = SelectedUser.Name,
                Surname = SelectedUser.Surname,
                Patron = SelectedUser.Patron,
                Role = SelectedUser.Role,
                RoleCode = SelectedUser.RoleCode,
                WorkshopId = SelectedUser.WorkshopId,
                WorkshopName = SelectedUser.WorkshopName,
                PersonnelNumber = SelectedUser.PersonnelNumber
            };

            if (!_dialogService.ShowUserDialog(editDto, WorkshopOptions, true))
                return;

            try
            {
                await _userService.UpdateUserAsync(editDto);
                await LoadUsersAsync();
                StatusMessage = "Пользователь обновлен.";
            }
            catch (Exception ex)
            {
                var message = $"Не удалось обновить пользователя: {GetErrorMessage(ex)}";
                StatusMessage = message;
                _dialogService.ShowMessage(message, "Ошибка");
            }
        }

        [RelayCommand]
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                StatusMessage = "Выберите пользователя.";
                return;
            }

            if (_authService.CurrentUser?.Id == SelectedUser.Id)
            {
                StatusMessage = "Нельзя удалить текущего пользователя.";
                return;
            }

            if (!_dialogService.ShowConfirm($"Удалить {SelectedUser.Surname} {SelectedUser.Name}?"))
                return;

            try
            {
                await _userService.DeleteUserAsync(SelectedUser.Id);
                SelectedUser = null;
                await LoadUsersAsync();
                StatusMessage = "Пользователь удален.";
            }
            catch (Exception ex)
            {
                var message = $"Не удалось удалить пользователя: {GetErrorMessage(ex)}";
                StatusMessage = message;
                _dialogService.ShowMessage(message, "Ошибка");
            }
        }

        private static string GetErrorMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null)
                current = current.InnerException;

            return current.Message;
        }

        private void RebuildFilterOptions()
        {
            RoleFilterOptions.Clear();
            RoleFilterOptions.Add(AllRolesFilter);
            foreach (var role in Users
                .Select(user => user.Role)
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct())
            {
                RoleFilterOptions.Add(role);
            }

            if (!RoleFilterOptions.Contains(SelectedRoleFilter))
                SelectedRoleFilter = AllRolesFilter;

            WorkshopFilterOptions.Clear();
            WorkshopFilterOptions.Add(AllWorkshopsFilter);
            foreach (var workshop in WorkshopOptions
                .Select(workshop => workshop.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct())
            {
                WorkshopFilterOptions.Add(workshop);
            }

            if (!WorkshopFilterOptions.Contains(SelectedWorkshopFilter))
                SelectedWorkshopFilter = AllWorkshopsFilter;
        }

        partial void OnSearchTextChanged(string value) => UsersView?.Refresh();

        partial void OnSelectedRoleFilterChanged(string value) => UsersView?.Refresh();

        partial void OnSelectedWorkshopFilterChanged(string value) => UsersView?.Refresh();

        private bool FilterUser(object item)
        {
            if (item is not UserProfileDto user)
                return false;

            var search = SearchText.Trim();
            if (!string.IsNullOrWhiteSpace(search)
                && !Contains(user.Surname, search)
                && !Contains(user.Name, search)
                && !Contains(user.Patron, search)
                && !Contains(user.PersonnelNumber, search)
                && !Contains(user.Role, search)
                && !Contains(user.WorkshopName, search))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SelectedRoleFilter)
                && SelectedRoleFilter != AllRolesFilter
                && !string.Equals(user.Role, SelectedRoleFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(SelectedWorkshopFilter)
                && SelectedWorkshopFilter != AllWorkshopsFilter
                && !string.Equals(user.WorkshopName, SelectedWorkshopFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool Contains(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
    }
}
