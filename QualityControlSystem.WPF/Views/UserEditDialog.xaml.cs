using System.Text.RegularExpressions;
using System.Windows;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Views
{
    public partial class UserEditDialog : Window
    {
        private static readonly Regex PersonNameRegex = new(@"^[А-ЯЁ][а-яё]+(-[А-ЯЁ][а-яё]+)*$", RegexOptions.Compiled);
        private static readonly Regex PersonnelNumberRegex = new(@"^\d{1,6}$", RegexOptions.Compiled);
        private readonly UserProfileDto _user;

        public UserEditDialog(UserProfileDto user, bool isEdit)
        {
            InitializeComponent();

            _user = user;
            Title = isEdit ? "Редактирование пользователя" : "Добавление пользователя";
            TitleText.Text = Title;

            SurnameTextBox.Text = _user.Surname;
            NameTextBox.Text = _user.Name;
            PatronTextBox.Text = _user.Patron;
            PersonnelNumberTextBox.Text = _user.PersonnelNumber;
            RoleComboBox.SelectedValue = string.IsNullOrWhiteSpace(_user.Role) ? "operator" : _user.Role;
            WorkshopNumberTextBox.Text = _user.WorkshopId.GetValueOrDefault() > 0 ? _user.WorkshopId.ToString() : string.Empty;
            PasswordHintText.Text = isEdit
                ? "Оставьте пароль пустым, чтобы не менять его."
                : "Если пароль не указан, будет использован default123.";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var surname = SurnameTextBox.Text.Trim();
            var name = NameTextBox.Text.Trim();
            var patron = PatronTextBox.Text.Trim();
            var personnelNumber = PersonnelNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(personnelNumber))
            {
                ShowValidationMessage("Заполните фамилию, имя и табельный номер.");
                return;
            }

            if (!IsValidPersonName(surname))
            {
                ShowValidationMessage("Фамилия должна начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");
                SurnameTextBox.Focus();
                return;
            }

            if (!IsValidPersonName(name))
            {
                ShowValidationMessage("Имя должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");
                NameTextBox.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(patron) && !IsValidPersonName(patron))
            {
                ShowValidationMessage("Отчество должно начинаться с заглавной русской буквы и содержать только русские буквы или дефис.");
                PatronTextBox.Focus();
                return;
            }

            if (!PersonnelNumberRegex.IsMatch(personnelNumber))
            {
                ShowValidationMessage("Табельный номер должен содержать от 1 до 6 цифр. Например: 123 или 000123.");
                PersonnelNumberTextBox.Focus();
                return;
            }

            if (RoleComboBox.SelectedValue is not string role)
            {
                ShowValidationMessage("Выберите роль пользователя.");
                RoleComboBox.Focus();
                return;
            }

            int? workshopId = null;
            if (!string.IsNullOrWhiteSpace(WorkshopNumberTextBox.Text))
            {
                if (!int.TryParse(WorkshopNumberTextBox.Text, out var parsedWorkshopId) || parsedWorkshopId <= 0)
                {
                    ShowValidationMessage("ID цеха должен быть положительным числом.");
                    WorkshopNumberTextBox.Focus();
                    return;
                }

                workshopId = parsedWorkshopId;
            }

            _user.Surname = surname;
            _user.Name = name;
            _user.Patron = string.IsNullOrWhiteSpace(patron) ? null : patron;
            _user.PersonnelNumber = personnelNumber;
            _user.Role = role;
            _user.WorkshopId = workshopId;
            _user.Password = PasswordBox.Password;

            DialogResult = true;
        }

        private static bool IsValidPersonName(string value)
        {
            return PersonNameRegex.IsMatch(value);
        }

        private static void ShowValidationMessage(string message)
        {
            MessageBox.Show(message, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
