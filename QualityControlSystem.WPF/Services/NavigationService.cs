using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Linq;
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
                var instance = Activator.CreateInstance(typeof(TView));

                if (instance is Window window)
                {
                    window.Show();
                }
                else if (instance is UserControl userControl)
                {
                    _mainWindow.MainContent.Content = userControl;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка навигации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GoBack()
        {
            if (Application.Current.Windows.Count > 1)
            {
                var currentWindow = Application.Current.Windows
                    .OfType<Window>()
                    .LastOrDefault();
                currentWindow?.Close();
            }
        }
    }
}