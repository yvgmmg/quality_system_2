using QualityControlSystem.Infrastructure.Repositories;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private UserProfileDto? _currentUser;
    public event Action<UserProfileDto?>? CurrentUserChanged;

    public UserProfileDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    private void OnCurrentUserChanged()
    {
        CurrentUserChanged?.Invoke(CurrentUser);
    }

    public async Task<bool> LoginAsync(string personnelNumber, string password)
    {
        var user = await _userRepository.ValidateCredentialsAsync(personnelNumber, password);
        if (user != null)
        {
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
            CurrentUserChanged?.Invoke(_currentUser);
            return true;
        }
        return false;
    }

    public void Logout()
    {
        _currentUser = null;
        CurrentUserChanged?.Invoke(null);
    }

}