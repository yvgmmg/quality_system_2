using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.Services.Interfaces;
using System.Data;

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
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await GetUserByPersonnelNumberAsync(dbContext, personnelNumber);

            if (user == null)
            {
                _dialogService.ShowMessage("Неправильный логин.", "Ошибка авторизации");
                return false;
            }

            if (!PasswordMatches(password, user.Password))
            {
                _dialogService.ShowMessage("Неправильный пароль.", "Ошибка авторизации");
                return false;
            }

            _currentUser = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Surname = user.Surname,
                Patron = user.Patron,
                Role = user.Role,
                RoleCode = user.RoleCode,
                WorkshopId = user.WorkshopId,
                WorkshopName = user.WorkshopName,
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

    private static async Task<UserAuthRow?> GetUserByPersonnelNumberAsync(AppDbContext dbContext, string personnelNumber)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                up.user_profile_id,
                up.last_name,
                up.first_name,
                up.middle_name,
                up.workshop_id,
                up.personnel_number,
                up.password,
                r.name AS role_name,
                r.role_code,
                w.number AS workshop_number,
                w.purpose AS workshop_purpose
            FROM public.user_profile up
            INNER JOIN public."role" r ON r.role_id = up.role_id
            LEFT JOIN public.workshop w ON w.workshop_id = up.workshop_id
            WHERE up.personnel_number = @personnel_number
            LIMIT 1;
            """;
        AddParameter(command, "personnel_number", personnelNumber);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new UserAuthRow
        {
            Id = reader.GetInt32(reader.GetOrdinal("user_profile_id")),
            Surname = reader.GetString(reader.GetOrdinal("last_name")),
            Name = reader.GetString(reader.GetOrdinal("first_name")),
            Patron = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString(reader.GetOrdinal("middle_name")),
            WorkshopId = reader.IsDBNull(reader.GetOrdinal("workshop_id")) ? null : reader.GetInt32(reader.GetOrdinal("workshop_id")),
            WorkshopName = FormatWorkshop(
                ReadNullableText(reader, "workshop_number"),
                ReadNullableText(reader, "workshop_purpose")),
            PersonnelNumber = reader.GetString(reader.GetOrdinal("personnel_number")),
            Password = reader.GetString(reader.GetOrdinal("password")),
            Role = reader.GetString(reader.GetOrdinal("role_name")),
            RoleCode = reader.GetString(reader.GetOrdinal("role_code"))
        };
    }

    private static bool PasswordMatches(string password, string storedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedPassword);
        }
        catch
        {
            return password == storedPassword;
        }
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string FormatWorkshop(string? number, string? purpose)
    {
        if (string.IsNullOrWhiteSpace(number))
            return string.Empty;

        return string.IsNullOrWhiteSpace(purpose)
            ? $"Цех {number}"
            : $"Цех {number} - {purpose}";
    }

    private static string? ReadNullableText(IDataRecord reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }

    private sealed class UserAuthRow
    {
        public int Id { get; set; }
        public string Surname { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Patron { get; set; }
        public int? WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;
        public string PersonnelNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
    }
}
