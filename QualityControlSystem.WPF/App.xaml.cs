using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QualityControlSystem.WPF.Services;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels;
using QualityControlSystem.WPF.Views;
using System;
using System.Windows;

namespace QualityControlSystem.WPF
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Unhandled exception: {ex?.Message}\n{ex?.StackTrace}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Dispatcher exception: {args.Exception.Message}\n{args.Exception.StackTrace}",
                                "Ошибка UI", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = false; // или true, чтобы не закрывать
            };

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IConfiguration>(context.Configuration);

                    // Существующие сервисы
                    services.AddSingleton<IApiClientService, ApiClientService>();
                    services.AddSingleton<IEdgeDeviceService, EdgeDeviceService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INotificationService, NotificationService>();

                    // НОВЫЕ СЕРВИСЫ аутентификации
                    services.AddSingleton<IAuthService, AuthService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();

                    // Views (UserControl и окна)
                    services.AddTransient<LoginView>();
                    services.AddTransient<DashboardView>();
                    services.AddSingleton<MainWindow>();

                    // NavigationService – обновлённая версия с IServiceProvider
                    services.AddSingleton<INavigationService>(sp =>
                        new NavigationService(
                            sp.GetRequiredService<MainWindow>(),
                            sp));
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                await AppHost!.StartAsync();
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}\n{ex.StackTrace}",
                                "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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