using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userService;
        private readonly IDialogService _dialogService;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private ObservableCollection<UserProfileDto> _users = new();

        [ObservableProperty]
        private UserProfileDto? _selectedUser;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public UserManagementViewModel(
            IUserManagementService userService,
            IDialogService dialogService,
            IAuthService authService)
        {
            _userService = userService;
            _dialogService = dialogService;
            _authService = authService;
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            IsBusy = true;
            StatusMessage = "Загрузка пользователей...";

            try
            {
                var users = await _userService.GetAllUsersAsync();
                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);

                StatusMessage = Users.Count == 0 ? "Пользователи не найдены." : $"Пользователей: {Users.Count}";
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
            var newUser = new UserProfileDto { Role = "Operator" };
            if (!_dialogService.ShowUserDialog(newUser, false))
                return;

            try
            {
                await _userService.AddUserAsync(newUser);
                await LoadUsersAsync();
                StatusMessage = "Пользователь добавлен. Пароль по умолчанию: default123";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось добавить пользователя: {GetErrorMessage(ex)}";
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
                WorkshopId = SelectedUser.WorkshopId,
                WorkshopNumber = SelectedUser.WorkshopNumber,
                PersonnelNumber = SelectedUser.PersonnelNumber
            };

            if (!_dialogService.ShowUserDialog(editDto, true))
                return;

            try
            {
                await _userService.UpdateUserAsync(editDto);
                await LoadUsersAsync();
                StatusMessage = "Пользователь обновлён.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось обновить пользователя: {GetErrorMessage(ex)}";
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
                StatusMessage = "Пользователь удалён.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Не удалось удалить пользователя: {GetErrorMessage(ex)}";
            }
        }

        private static string GetErrorMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null)
                current = current.InnerException;

            return current.Message;
        }
    }
}
