// QualityControlSystem.WPF/Services/UserManagementService.cs
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.Infrastructure.Enums;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using BCrypt.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;

        public UserManagementService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => new UserProfileDto
            {
                Id = u.UserProfileId,
                Name = u.Name,
                Surname = u.Surname,
                Patron = u.Patron,
                Role = u.Role.ToString(),
                WorkshopId = u.WorkshopId,
                PersonnelNumber = u.PersonnelNumber
            });
        }

        public async Task<UserProfileDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return null;
            return new UserProfileDto
            {
                Id = u.UserProfileId,
                Name = u.Name,
                Surname = u.Surname,
                Patron = u.Patron,
                Role = u.Role.ToString(),
                WorkshopId = u.WorkshopId,
                PersonnelNumber = u.PersonnelNumber
            };
        }

        public async Task<bool> AddUserAsync(UserProfileDto user, string defaultPassword = "default123")
        {
            var entity = new UserProfile
            {
                Name = user.Name,
                Surname = user.Surname,
                Patron = user.Patron,
                PersonnelNumber = user.PersonnelNumber,
                WorkshopId = user.WorkshopId,
                Role = Enum.Parse<UserRole>(user.Role),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword)
            };
            await _userRepository.AddAsync(entity);
            return true;
        }

        public async Task<bool> UpdateUserAsync(UserProfileDto user)
        {
            var existing = await _userRepository.GetByIdAsync(user.Id);
            if (existing == null) return false;
            existing.Name = user.Name;
            existing.Surname = user.Surname;
            existing.Patron = user.Patron;
            existing.PersonnelNumber = user.PersonnelNumber;
            existing.WorkshopId = user.WorkshopId;
            existing.Role = Enum.Parse<UserRole>(user.Role);
            await _userRepository.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            await _userRepository.DeleteAsync(id);
            return true;
        }
    }
}