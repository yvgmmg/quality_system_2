using Microsoft.Extensions.Configuration;
using QualityControlSystem.WPF.Services.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QualityControlSystem.WPF.Services
{
    public class ApiClientService : IApiClientService
    {
        private readonly HttpClient _httpClient;

        public ApiClientService(IConfiguration configuration)
        {
            var baseUrl = configuration["EdgeDevice:BaseUrl"] ?? "http://192.168.1.100:5000";
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _httpClient.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue<int>("EdgeDevice:TimeoutSeconds", 30));
        }

        public async Task<TResponse> GetAsync<TResponse>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(json)!;
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(json)!;
        }

        public async Task PostAsync<TRequest>(string endpoint, TRequest request)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();
        }
    }
}