using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserManagementService _userService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<UserProfileDto> _users = new();

        [ObservableProperty]
        private UserProfileDto? _selectedUser;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public UserManagementViewModel(IUserManagementService userService, IDialogService dialogService)
        {
            _userService = userService;
            _dialogService = dialogService;
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in users)
                Users.Add(u);
        }

        [RelayCommand]
        private async Task AddUserAsync()
        {
            var newUser = new UserProfileDto();
            if (_dialogService.ShowUserDialog(newUser, false))
            {
                await _userService.AddUserAsync(newUser);
                await LoadUsersAsync();
                StatusMessage = "Пользователь добавлен. Пароль по умолчанию: default123";
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
                PersonnelNumber = SelectedUser.PersonnelNumber
            };

            if (_dialogService.ShowUserDialog(editDto, true))
            {
                await _userService.UpdateUserAsync(editDto);
                await LoadUsersAsync();
                StatusMessage = "Пользователь обновлён.";
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

            if (_dialogService.ShowConfirm($"Удалить {SelectedUser.Surname} {SelectedUser.Name}?"))
            {
                await _userService.DeleteUserAsync(SelectedUser.Id);
                await LoadUsersAsync();
                StatusMessage = "Пользователь удалён.";
            }
        }
    }
}