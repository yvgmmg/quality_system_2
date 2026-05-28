using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Models;
using System;
using System.Threading.Tasks;

using QualityControlSystem.WPF.ViewModels;
using System.Windows;

namespace QualityControlSystem.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}