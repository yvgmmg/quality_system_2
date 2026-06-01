using System.Windows;
using System.Windows.Controls;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Views
{
    public partial class UserEditDialog : Window
    {
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
            RoleComboBox.SelectedValue = string.IsNullOrWhiteSpace(_user.Role) ? "Operator" : _user.Role;
            WorkshopNumberInput.Text = _user.WorkshopNumber > 0 ? _user.WorkshopNumber.ToString() : string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SurnameTextBox.Text) ||
                string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PersonnelNumberTextBox.Text))
            {
                MessageBox.Show("Заполните фамилию, имя и табельный номер.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RoleComboBox.SelectedValue is not string role)
            {
                MessageBox.Show("Выберите роль пользователя.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(WorkshopNumberInput.Text, out var workshopNumber) || workshopNumber <= 0)
            {
                MessageBox.Show("Номер цеха должен быть положительным числом.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _user.Surname = SurnameTextBox.Text.Trim();
            _user.Name = NameTextBox.Text.Trim();
            _user.Patron = string.IsNullOrWhiteSpace(PatronTextBox.Text) ? null : PatronTextBox.Text.Trim();
            _user.PersonnelNumber = PersonnelNumberTextBox.Text.Trim();
            _user.Role = role;
            _user.WorkshopNumber = workshopNumber;
            _user.Password = PasswordBox.Password;

            DialogResult = true;
        }

        private TextBox WorkshopNumberInput => (TextBox)FindName("WorkshopNumberTextBox");
    }
}
