using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QualityControlSystem.WPF.Services;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels;
using System;
using System.Windows;

namespace QualityControlSystem.WPF
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConfiguration>(context.Configuration);

                    // Сервисы
                    services.AddSingleton<IApiClientService, ApiClientService>();
                    services.AddSingleton<IEdgeDeviceService, EdgeDeviceService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INotificationService, NotificationService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();

                    // Окна
                    services.AddSingleton<MainWindow>();

                    // NavigationService регистрируем после MainWindow
                    services.AddSingleton<INavigationService>(sp =>
                        new NavigationService(sp.GetRequiredService<MainWindow>()));
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost!.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}