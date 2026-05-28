using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services;

public class AuthService : IAuthService
{
    private UserProfileDto? _currentUser;

    // Демонстрационные данные (позже заменить на запрос к БД)
    private readonly List<(string Login, string Password, UserProfileDto Profile)> _users = new()
    {
        ("admin", "admin", new UserProfileDto { Id = 1, Name = "Админ", Surname = "Админов", Role = "admin", WorkshopId = 0, PersonnelNumber = "A000001" }),
        ("operator", "operator", new UserProfileDto { Id = 2, Name = "Иван", Surname = "Петров", Role = "operator", WorkshopId = 1, PersonnelNumber = "O123456" }),
        ("equipment", "equipment", new UserProfileDto { Id = 3, Name = "Сергей", Surname = "Сидоров", Role = "equipment specialist", WorkshopId = 1, PersonnelNumber = "E789012" }),
        ("qcofficer", "qcofficer", new UserProfileDto { Id = 4, Name = "Мария", Surname = "Иванова", Role = "quality control officer", WorkshopId = 1, PersonnelNumber = "Q345678" })
    };

    public Task<bool> LoginAsync(string login, string password)
    {
        var user = _users.FirstOrDefault(u => u.Login == login && u.Password == password);
        if (user != default)
        {
            _currentUser = user.Profile;
            return Task.FromResult(true);
        }
        _currentUser = null;
        return Task.FromResult(false);
    }

    public void Logout()
    {
        _currentUser = null;
    }

    public UserProfileDto? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
}