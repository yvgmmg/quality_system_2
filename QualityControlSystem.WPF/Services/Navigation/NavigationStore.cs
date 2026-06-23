using System;
using Microsoft.Extensions.DependencyInjection;
using QualityControlSystem.WPF.ViewModels.Base;

namespace QualityControlSystem.WPF.Services.Navigation;

public class NavigationStore : IDisposable
{
    private BaseViewModel? _currentViewModel;
    private IServiceScope? _currentScope;

    public event Action<BaseViewModel?>? CurrentViewModelChanged;

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
    }

    public void SetCurrentViewModel(BaseViewModel? viewModel, IServiceScope? scope)
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

    public void Dispose()
    {
        _currentScope?.Dispose();
        _currentScope = null;
        _currentViewModel = null;
    }
}
