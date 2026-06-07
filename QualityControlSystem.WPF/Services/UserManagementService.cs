using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace QualityControlSystem.WPF.Services
{
    public class UserManagementService : IUserManagementService
    {
        private static readonly Dictionary<string, string> RoleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "admin",
            ["operator"] = "operator",
            ["equipment specialist"] = "equipment specialist",
            ["quality control"] = "quality control",
            ["quality control officer"] = "quality control",
            ["quality controll officer"] = "quality control",
            ["quality control opfficer"] = "quality control",
            ["quality controll opfficer"] = "quality control"
        };

        private static readonly Dictionary<string, string> RoleCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "01000001",
            ["operator"] = "02000001",
            ["equipment specialist"] = "03000001",
            ["quality control"] = "04000001"
        };

        private static readonly Regex PersonNameRegex = new(@"^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$", RegexOptions.Compiled);
        private static readonly Regex PersonnelNumberRegex = new(@"^\d{1,6}$", RegexOptions.Compiled);

        private readonly AppDbContext _dbContext;
        private readonly IAuthService _authService;

        public UserManagementService(AppDbContext dbContext, IAuthService authService)
        {
            _dbContext = dbContext;
            _authService = authService;
        }

        public async Task<IEnumerable<UserProfileDto>> GetAllUsersAsync()
        {
            var users = new List<UserProfileDto>();
            var connection = await GetOpenConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    up.user_profile_id,
                    up.last_name,
                    up.first_name,
                    up.middle_name,
                    up.workshop_id,
                    up.personnel_number,
                    r.name AS role_name,
                    r.role_code
                FROM public.user_profile up
                INNER JOIN public."role" r ON r.role_id = up.role_id
                ORDER BY up.user_profile_id;
                """;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(ReadUser(reader));

            return users;
        }

        public async Task<UserProfileDto?> GetUserByIdAsync(int id)
        {
            var connection = await GetOpenConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    up.user_profile_id,
                    up.last_name,
                    up.first_name,
                    up.middle_name,
                    up.workshop_id,
                    up.personnel_number,
                    r.name AS role_name,
                    r.role_code
                FROM public.user_profile up
                INNER JOIN public."role" r ON r.role_id = up.role_id
                WHERE up.user_profile_id = @id
                LIMIT 1;
                """;
            AddParameter(command, "id", id);

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? ReadUser(reader) : null;
        }

        public async Task<bool> AddUserAsync(UserProfileDto user, string defaultPassword = "default123")
        {
            var role = await ValidateUserAsync(user);
            var roleId = await GetOrCreateRoleIdAsync(role);
            var passwordToHash = string.IsNullOrWhiteSpace(user.Password) ? defaultPassword : user.Password;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordToHash);

            var connection = await GetOpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO public.user_profile
                    (last_name, first_name, middle_name, personnel_number, workshop_id, role_id, password)
                VALUES
                    (@last_name, @first_name, @middle_name, @personnel_number, @workshop_id, @role_id, @password);
                """;
            AddUserParameters(command, user, roleId, passwordHash);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(UserProfileDto user)
        {
            var role = await ValidateUserAsync(user);
            var roleId = await GetOrCreateRoleIdAsync(role);
            var existingPassword = await GetPasswordHashAsync(user.Id);
            if (existingPassword == null)
                return false;

            var passwordHash = string.IsNullOrWhiteSpace(user.Password)
                ? existingPassword
                : BCrypt.Net.BCrypt.HashPassword(user.Password);

            var connection = await GetOpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE public.user_profile
                SET last_name = @last_name,
                    first_name = @first_name,
                    middle_name = @middle_name,
                    personnel_number = @personnel_number,
                    workshop_id = @workshop_id,
                    role_id = @role_id,
                    password = @password
                WHERE user_profile_id = @id;
                """;
            AddParameter(command, "id", user.Id);
            AddUserParameters(command, user, roleId, passwordHash);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            if (_authService.CurrentUser?.Id == id)
                throw new InvalidOperationException("Нельзя удалить текущего пользователя.");

            var connection = await GetOpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM public.user_profile WHERE user_profile_id = @id;";
            AddParameter(command, "id", id);
            await command.ExecuteNonQueryAsync();
            return true;
        }

        private async Task<string> ValidateUserAsync(UserProfileDto user)
        {
            if (!IsValidPersonName(user.Surname))
                throw new InvalidOperationException("Фамилия должна начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!IsValidPersonName(user.Name))
                throw new InvalidOperationException("Имя должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!string.IsNullOrWhiteSpace(user.Patron) && !IsValidPersonName(user.Patron))
                throw new InvalidOperationException("Отчество должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");

            if (!PersonnelNumberRegex.IsMatch(user.PersonnelNumber ?? string.Empty))
                throw new InvalidOperationException("Табельный номер должен содержать от 1 до 6 цифр. Например: 123 или 000123.");

            var role = NormalizeRole(user.Role);
            if (role == null)
                throw new InvalidOperationException("Выбрана некорректная роль пользователя.");

            if (user.WorkshopId.HasValue && !await WorkshopExistsAsync(user.WorkshopId.Value))
                throw new InvalidOperationException($"Цех с ID {user.WorkshopId.Value} не найден.");

            return role;
        }

        private async Task<bool> WorkshopExistsAsync(int workshopId)
        {
            var connection = await GetOpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM public.workshop WHERE workshop_id = @workshop_id;";
            AddParameter(command, "workshop_id", workshopId);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        private async Task<int> GetOrCreateRoleIdAsync(string role)
        {
            var connection = await GetOpenConnectionAsync();
            var roleCode = RoleCodes.TryGetValue(role, out var code)
                ? code
                : throw new InvalidOperationException($"Для роли {role} не задан код.");

            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = """
                    INSERT INTO public."role" (role_code, name)
                    VALUES (@role_code, @name)
                    ON CONFLICT (name) DO UPDATE SET role_code = EXCLUDED.role_code;
                    """;
                AddParameter(insert, "role_code", roleCode);
                AddParameter(insert, "name", role);
                await insert.ExecuteNonQueryAsync();
            }

            using var select = connection.CreateCommand();
            select.CommandText = "SELECT role_id FROM public.\"role\" WHERE name = @name;";
            AddParameter(select, "name", role);
            var result = await select.ExecuteScalarAsync();
            return result == null
                ? throw new InvalidOperationException($"Роль {role} не найдена.")
                : Convert.ToInt32(result);
        }

        private async Task<string?> GetPasswordHashAsync(int userId)
        {
            var connection = await GetOpenConnectionAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT password FROM public.user_profile WHERE user_profile_id = @id;";
            AddParameter(command, "id", userId);
            return await command.ExecuteScalarAsync() as string;
        }

        private async Task<DbConnection> GetOpenConnectionAsync()
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            return connection;
        }

        private static UserProfileDto ReadUser(IDataRecord reader)
        {
            return new UserProfileDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("user_profile_id")),
                Surname = reader.GetString(reader.GetOrdinal("last_name")),
                Name = reader.GetString(reader.GetOrdinal("first_name")),
                Patron = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
                WorkshopId = reader.IsDBNull(reader.GetOrdinal("workshop_id")) ? null : reader.GetInt32(reader.GetOrdinal("workshop_id")),
                PersonnelNumber = reader.GetString(reader.GetOrdinal("personnel_number")),
                Role = reader.GetString(reader.GetOrdinal("role_name")),
                RoleCode = reader.GetString(reader.GetOrdinal("role_code"))
            };
        }

        private static void AddUserParameters(IDbCommand command, UserProfileDto user, int roleId, string passwordHash)
        {
            AddParameter(command, "last_name", user.Surname);
            AddParameter(command, "first_name", user.Name);
            AddParameter(command, "middle_name", user.Patron);
            AddParameter(command, "personnel_number", user.PersonnelNumber);
            AddParameter(command, "workshop_id", user.WorkshopId);
            AddParameter(command, "role_id", roleId);
            AddParameter(command, "password", passwordHash);
        }

        private static void AddParameter(IDbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static string? NormalizeRole(string? role)
        {
            return string.IsNullOrWhiteSpace(role)
                ? null
                : RoleAliases.GetValueOrDefault(role.Trim());
        }

        private static bool IsValidPersonName(string? value)
        {
            return PersonNameRegex.IsMatch(value ?? string.Empty);
        }
    }
}
