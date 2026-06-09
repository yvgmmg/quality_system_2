using System.Threading.Tasks;

namespace QualityControlSystem.WPF.ViewModels.Base;

public interface IAsyncInitializable
{
    Task InitializeAsync();
}
