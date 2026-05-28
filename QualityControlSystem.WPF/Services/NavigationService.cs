using System;
using System.Windows;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services
{
    public class NavigationService : INavigationService
    {
        private readonly MainWindow _mainWindow;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(MainWindow mainWindow, IServiceProvider serviceProvider)
        {
            _mainWindow = mainWindow;
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<TView>() where TView : class
        {
            try
            {
                var view = _serviceProvider.GetService(typeof(TView)) as UIElement;
                if (view == null)
                {
                    MessageBox.Show($"Ошибка: {typeof(TView).Name} не зарегистрирован в DI.",
                                    "Навигация", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (_mainWindow.MainContent == null)
                {
                    MessageBox.Show("Ошибка: MainContent не найден в MainWindow.",
                                    "Навигация", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _mainWindow.MainContent.Content = view;
                // Дополнительно принудительно обновляем
                _mainWindow.MainContent.UpdateLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Исключение в NavigateTo: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GoBack() { }
    }
}