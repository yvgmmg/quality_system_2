using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QualityControlSystem.WPF.Services;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.ViewModels;
using QualityControlSystem.WPF.Views;
using System;
using System.Windows;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite(connectionString));

                    // Репозитории
                    services.AddScoped<IUserRepository, UserRepository>();

                    // Сервисы
                    services.AddSingleton<IApiClientService, ApiClientService>();
                    services.AddSingleton<IEdgeDeviceService, EdgeDeviceService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<IAuthService, AuthService>();

                    // NavigationService (без фабрики)
                    services.AddSingleton<INavigationService, NavigationService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();   // если нужен, иначе удалите
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();

                    // Views
                    services.AddTransient<LoginView>();
                    services.AddTransient<DashboardView>();
                    services.AddSingleton<MainWindow>();      // теперь без параметров
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            var navigationService = AppHost.Services.GetRequiredService<INavigationService>();

            navigationService.Initialize(mainWindow);
            mainWindow.Show();

            navigationService.NavigateTo<LoginView>();

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