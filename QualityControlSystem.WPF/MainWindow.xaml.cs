using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Models;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels;
using QualityControlSystem.WPF.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace QualityControlSystem.WPF
{
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;

        public MainWindow(MainViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _navigationService = navigationService;
            Loaded += (s, e) =>
            {
                var loginView = new LoginView(new LoginViewModel(null, null));
                MainContent.Content = loginView;
            };
        }
    }
}