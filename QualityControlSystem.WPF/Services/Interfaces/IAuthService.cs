using System;
using QualityControlSystem.WPF.Dtos;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string personnelNumber, string password);
        void Logout();
        UserProfileDto? CurrentUser { get; }
        event Action<UserProfileDto?>? CurrentUserChanged;
    }
}