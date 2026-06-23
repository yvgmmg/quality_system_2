// QualityControlSystem.WPF/Services/Interfaces/IUserManagementService.cs
using QualityControlSystem.WPF.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<IEnumerable<UserProfileDto>> GetAllUsersAsync();
        Task<IReadOnlyList<LookupItemDto>> GetWorkshopOptionsAsync();
        Task<UserProfileDto?> GetUserByIdAsync(int id);
        Task<bool> AddUserAsync(UserProfileDto user, string defaultPassword = "default123");
        Task<bool> UpdateUserAsync(UserProfileDto user);
        Task<bool> DeleteUserAsync(int id);
    }
}
