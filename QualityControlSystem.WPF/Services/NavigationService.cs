using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels;
using QualityControlSystem.WPF.Views;
using System;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private MainWindow? _mainWindow;
        private MainViewModel? _mainViewModel;

        public event Action<UserControl>? CurrentViewChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _mainViewModel = _mainWindow.DataContext as MainViewModel;
        }

        public void NavigateTo<TView>() where TView : UserControl
        {
            var view = _serviceProvider.GetRequiredService<TView>();
            SetView(view);
        }

        public void NavigateTo(Type viewType)
        {
            var view = _serviceProvider.GetRequiredService(viewType) as UserControl;
            if (view != null)
                SetView(view);
        }
        private void SetView(UserControl view)
        {
            if (_mainViewModel != null)
                _mainViewModel.CurrentView = view;
            CurrentViewChanged?.Invoke(view);
        }
    }
}