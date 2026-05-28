using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services.Interfaces
{
    public interface IApiClientService
    {
        Task<TResponse> GetAsync<TResponse>(string endpoint);
        Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request);
        Task PostAsync<TRequest>(string endpoint, TRequest request);
    }
}