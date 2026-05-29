using QualityControlSystem.Infrastructure.Repositories;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private UserProfileDto? _currentUser;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> LoginAsync(string login, string password)
    {
        var isValid = await _userRepository.ValidateCredentialsAsync(login, password);
        if (!isValid) return false;

        var user = await _userRepository.GetByPersonnelNumberAsync(login);
        if (user == null) return false;

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
        return true;
    }

    public void Logout() => _currentUser = null;
    public UserProfileDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
}