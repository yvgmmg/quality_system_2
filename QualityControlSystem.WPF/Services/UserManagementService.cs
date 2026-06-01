using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Enums;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;

        public UserManagementService(
            IUserRepository userRepository,
            AppDbContext dbContext,
            IAuthService authService)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
            _authService = authService;
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
        {
            var users = await _dbContext.UserProfiles
                .Include(u => u.Workshop)
                .ToListAsync();
            return users.Select(u => new UserProfileDto
            {
                Id = u.UserProfileId,
                Name = u.Name,
                Surname = u.Surname,
                Patron = u.Patron,
                Role = u.Role.ToString(),
                WorkshopId = u.WorkshopId,
                WorkshopNumber = u.Workshop.Number,
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
                WorkshopNumber = await GetWorkshopNumberAsync(u.WorkshopId),
                PersonnelNumber = u.PersonnelNumber
            };
        }

        public async Task<bool> AddUserAsync(UserProfileDto user, string defaultPassword = "default123")
        {
            await ValidateUserAsync(user);

            var role = ToDatabaseRole(user.Role);
            user.WorkshopId = await GetWorkshopIdByNumberAsync(user.WorkshopNumber);
            // Use provided password if not empty, otherwise fallback to defaultPassword
            var passwordToHash = string.IsNullOrWhiteSpace(user.Password) ? defaultPassword : user.Password;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

            await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO public.user_profile
                    (""name"", surname, patron, workshop_id, password_hash, personnel_number, ""role"")
                VALUES
                    ({user.Name}, {user.Surname}, {user.Patron}, {user.WorkshopId}, {passwordHash}, {user.PersonnelNumber}, CAST({role} AS public.role))");

            return true;
        }

        public async Task<bool> UpdateUserAsync(UserProfileDto user)
        {
            await ValidateUserAsync(user);

            var existing = await _userRepository.GetByIdAsync(user.Id);
            if (existing == null) return false;

            var role = ToDatabaseRole(user.Role);
            user.WorkshopId = await GetWorkshopIdByNumberAsync(user.WorkshopNumber);
            // Determine password hash: if a new password is provided, hash it; otherwise keep existing hash
            var passwordHash = !string.IsNullOrWhiteSpace(user.Password)
                ? BCrypt.Net.BCrypt.HashPassword(user.Password)
                : existing.PasswordHash;

            await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE public.user_profile
                SET ""name"" = {user.Name},
                    surname = {user.Surname},
                    patron = {user.Patron},
                    personnel_number = {user.PersonnelNumber},
                    workshop_id = {user.WorkshopId},
                    password_hash = {passwordHash},
                    ""role"" = CAST({role} AS public.role)
                WHERE user_profile_id = {user.Id}");

            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            if (_authService.CurrentUser?.Id == id)
                throw new InvalidOperationException("Нельзя удалить текущего пользователя.");

            await _userRepository.DeleteAsync(id);
            return true;
        }

        private async Task ValidateUserAsync(UserProfileDto user)
        {
            if (!IsValidPersonName(user.Surname))
                throw new InvalidOperationException("Фамилия должна начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!IsValidPersonName(user.Name))
                throw new InvalidOperationException("Имя должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!string.IsNullOrWhiteSpace(user.Patron) && !IsValidPersonName(user.Patron))
                throw new InvalidOperationException("Отчество должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!Regex.IsMatch(user.PersonnelNumber ?? string.Empty, @"^[А-ЯЁа-яёA-Za-z]\d+$"))
                throw new InvalidOperationException("Табельный номер должен начинаться с буквы, затем должны идти цифры. Например: A123.");

            if (!Enum.TryParse<UserRole>(user.Role, out _))
                throw new InvalidOperationException("Выбрана некорректная роль пользователя.");

            if (user.WorkshopNumber <= 0)
                throw new InvalidOperationException("Номер цеха должен быть положительным числом.");

            var workshopExists = await _dbContext.Workshops.AnyAsync(w => w.Number == user.WorkshopNumber);
            if (!workshopExists)
                throw new InvalidOperationException($"Цех с номером {user.WorkshopNumber} не найден.");
        }

        private async Task<int> GetWorkshopIdByNumberAsync(int workshopNumber)
        {
            var workshop = await _dbContext.Workshops
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Number == workshopNumber);

            if (workshop == null)
                throw new InvalidOperationException($"Цех с номером {workshopNumber} не найден.");

            return workshop.WorkshopId;
        }

        private async Task<int> GetWorkshopNumberAsync(int workshopId)
        {
            return await _dbContext.Workshops
                .Where(w => w.WorkshopId == workshopId)
                .Select(w => w.Number)
                .FirstOrDefaultAsync();
        }

        private static bool IsValidPersonName(string? value)
        {
            return Regex.IsMatch(value ?? string.Empty, @"^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$");
        }

        private static string ToDatabaseRole(string role)
        {
            return Enum.Parse<UserRole>(role) switch
            {
                UserRole.Admin => "admin",
                UserRole.Operator => "operator",
                UserRole.EquipmentSpecialist => "equipment specialist",
                UserRole.QualityControlOfficer => "quality control officer",
                _ => throw new InvalidOperationException("Выбрана некорректная роль пользователя.")
            };
        }
    }
}
