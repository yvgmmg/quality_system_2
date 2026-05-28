using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Services
{
    public class NavigationService : INavigationService
    {
        private readonly MainWindow _mainWindow;

        public NavigationService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void NavigateTo<TView>() where TView : class
        {
            try
            {
                var view = Activator.CreateInstance(typeof(TView)) as Window ??
                          Activator.CreateInstance(typeof(TView)) as UserControl;

                if (view is Window window)
                {
                    window.Show();
                }
                else if (view is UserControl userControl)
                {
                    // Для ContentControl в MainWindow (рекомендуемый подход)
                    if (_mainWindow.MainContent != null)
                    {
                        _mainWindow.MainContent.Content = userControl;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GoBack()
        {
            // Простая реализация - можно расширить позже
            if (Application.Current.Windows.Count > 1)
            {
                var currentWindow = Application.Current.Windows.OfType<Window>().LastOrDefault();
                currentWindow?.Close();
            }
        }
    }
}