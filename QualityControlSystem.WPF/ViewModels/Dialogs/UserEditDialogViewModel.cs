using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Dtos;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.ViewModels.Dialogs;

public partial class UserEditDialogViewModel : BaseViewModel
{
    private static readonly Regex PersonNameRegex = new(@"^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$", RegexOptions.Compiled);
    private static readonly Regex PersonnelNumberRegex = new(@"^\d{1,6}$", RegexOptions.Compiled);

    private readonly UserProfileDto _user;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _surname = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _patron = string.Empty;

    [ObservableProperty]
    private string _personnelNumber = string.Empty;

    [ObservableProperty]
    private string _selectedRole = "operator";

    [ObservableProperty]
    private ObservableCollection<LookupItemDto> _workshopOptions = new();

    [ObservableProperty]
    private LookupItemDto? _selectedWorkshop;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _passwordHint = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public UserEditDialogViewModel(UserProfileDto user, IEnumerable<LookupItemDto> workshops, bool isEdit)
    {
        _user = user;
        Title = isEdit ? "Редактирование пользователя" : "Добавление пользователя";
        Surname = user.Surname;
        Name = user.Name;
        Patron = user.Patron ?? string.Empty;
        PersonnelNumber = user.PersonnelNumber;
        SelectedRole = string.IsNullOrWhiteSpace(user.Role) ? "operator" : user.Role;
        WorkshopOptions = new ObservableCollection<LookupItemDto>(workshops);
        SelectedWorkshop = user.WorkshopId.HasValue
            ? WorkshopOptions.FirstOrDefault(workshop => workshop.Id == user.WorkshopId.Value)
            : null;
        PasswordHint = isEdit
            ? "Оставьте пароль пустым, чтобы не менять его."
            : "Если пароль не указан, будет использован default123.";
    }

    public ObservableCollection<UserRoleOption> RoleOptions { get; } =
    [
        new("Администратор", "admin"),
        new("Оператор", "operator"),
        new("Специалист по оборудованию", "equipment specialist"),
        new("Контролер качества", "quality control")
    ];

    public event Action<bool?>? CloseRequested;

    [RelayCommand]
    private void Save()
    {
        var surname = Surname.Trim();
        var name = Name.Trim();
        var patron = Patron.Trim();
        var personnelNumber = PersonnelNumber.Trim();

        if (string.IsNullOrWhiteSpace(surname) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(personnelNumber))
        {
            ErrorMessage = "Заполните фамилию, имя и табельный номер.";
            return;
        }

        if (!IsValidPersonName(surname))
        {
            ErrorMessage = "Фамилия должна начинаться с заглавной русской буквы и содержать только русские буквы или дефис.";
            return;
        }

        if (!IsValidPersonName(name))
        {
            ErrorMessage = "Имя должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(patron) && !IsValidPersonName(patron))
        {
            ErrorMessage = "Отчество должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.";
            return;
        }

        if (!PersonnelNumberRegex.IsMatch(personnelNumber))
        {
            ErrorMessage = "Табельный номер должен содержать от 1 до 6 цифр. Например: 123 или 000123.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            ErrorMessage = "Выберите роль пользователя.";
            return;
        }

        _user.Surname = surname;
        _user.Name = name;
        _user.Patron = string.IsNullOrWhiteSpace(patron) ? null : patron;
        _user.PersonnelNumber = personnelNumber;
        _user.Role = SelectedRole;
        _user.WorkshopId = SelectedWorkshop?.Id;
        _user.Password = Password;
        ErrorMessage = string.Empty;

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    private static bool IsValidPersonName(string value)
    {
        return PersonNameRegex.IsMatch(value);
    }
}

public sealed record UserRoleOption(string Title, string Value);
