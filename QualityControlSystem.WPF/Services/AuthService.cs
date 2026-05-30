using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using System.Windows;

namespace QualityControlSystem.WPF.Services;

public class AuthService : IAuthService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDialogService _dialogService;
    private UserProfileDto? _currentUser;

    public event Action<UserProfileDto?>? CurrentUserChanged;
    public UserProfileDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public AuthService(IServiceScopeFactory scopeFactory, IDialogService dialogService)
    {
        _scopeFactory = scopeFactory;
        _dialogService = dialogService;
    }

    private void OnCurrentUserChanged() => CurrentUserChanged?.Invoke(CurrentUser);

    public async Task<bool> LoginAsync(string personnelNumber, string password)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.ValidateCredentialsAsync(personnelNumber, password);
            if (user == null)
            {
                // Determine whether login (personnel number) exists
                var userExists = await userRepository.GetByPersonnelNumberAsync(personnelNumber) != null;
                if (userExists)
                {
                    _dialogService.ShowMessage("Неправильный пароль.", "Ошибка авторизации");
                }
                else
                {
                    _dialogService.ShowMessage("Неправильный логин.", "Ошибка авторизации");
                }
                return false;
            }

            _currentUser = new UserProfileDto
            {
                Id = user.UserProfileId,
                Name = user.Name,
                Surname = user.Surname,
                Patron = user.Patron,
                Role = user.Role.ToString(),
                WorkshopId = user.WorkshopId,
                PersonnelNumber = user.PersonnelNumber
            };
            OnCurrentUserChanged();
            return true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage($"Ошибка авторизации: {ex.Message}\n{ex.StackTrace}", "Auth Error");
            return false;
        }
    }

    public void Logout()
    {
        _currentUser = null;
        OnCurrentUserChanged();
    }
}