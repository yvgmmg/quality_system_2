using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QualityControlSystem.WPF.ViewModels.Base;
using System.Windows.Controls;

namespace QualityControlSystem.WPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        [ObservableProperty]
        private UserControl? _currentView;

        [ObservableProperty]
        private string _currentUserName = string.Empty;

        public MainViewModel()
        {
        }
    }
}