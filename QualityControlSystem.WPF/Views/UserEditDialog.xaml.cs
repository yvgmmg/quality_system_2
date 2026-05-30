using System.Windows;
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
            WorkshopIdTextBox.Text = _user.WorkshopId > 0 ? _user.WorkshopId.ToString() : string.Empty;
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

            if (!int.TryParse(WorkshopIdTextBox.Text, out var workshopId) || workshopId <= 0)
            {
                MessageBox.Show("ID цеха должен быть положительным числом.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, существует ли цех с указанным ID
            if (!WorkshopExists(workshopId))
            {
                MessageBox.Show($"Цех с ID {workshopId} не найден.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _user.Surname = SurnameTextBox.Text.Trim();
            _user.Name = NameTextBox.Text.Trim();
            _user.Patron = string.IsNullOrWhiteSpace(PatronTextBox.Text) ? null : PatronTextBox.Text.Trim();
            _user.PersonnelNumber = PersonnelNumberTextBox.Text.Trim();
            _user.Role = role;
            _user.WorkshopId = workshopId;
            _user.Password = PasswordBox.Password;

            DialogResult = true;
        }

        private bool WorkshopExists(int workshopId)
        {
            try
            {
                using var dbContext = new QualityControlSystem.Infrastructure.AppDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QualityControlSystem.Infrastructure.AppDbContext>().Options);
                return dbContext.Workshops.Any(w => w.WorkshopId == workshopId);
            }
            catch
            {
                return false;
            }
        }
    }
}
