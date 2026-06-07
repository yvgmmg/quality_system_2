using System;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.Services.Navigation;

public class NavigationStore
{
    private BaseViewModel? _currentViewModel;
    private IDisposable? _currentScope;

    public event Action<BaseViewModel?>? CurrentViewModelChanged;

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
    }

    public void SetCurrentViewModel(BaseViewModel? viewModel, IDisposable? scope)
    {
        if (ReferenceEquals(_currentViewModel, viewModel))
        {
            scope?.Dispose();
            return;
        }

        _currentScope?.Dispose();
        _currentScope = scope;
        _currentViewModel = viewModel;
        CurrentViewModelChanged?.Invoke(viewModel);
    }
}
