using System;
using System.Windows;
using QualityControlSystem.WPF.Services.Interfaces;

namespace QualityControlSystem.WPF.Services
{
    public class NavigationService : INavigationService
    {
        private MainWindow? _mainWindow;
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void NavigateTo<TView>() where TView : class
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("NavigationService не инициализирован. Вызовите Initialize() перед навигацией.");

            var view = _serviceProvider.GetService(typeof(TView)) as UIElement;
            if (view == null)
                throw new InvalidOperationException($"View {typeof(TView).Name} не зарегистрирован в DI.");

            if (_mainWindow.MainContent != null)
                _mainWindow.MainContent.Content = view;
            else
                MessageBox.Show("MainContent не найден в окне", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void GoBack() { }
    }
}