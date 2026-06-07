using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QualityControlSystem.Infrastructure;
using QualityControlSystem.Infrastructure.Repositories;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;
using QualityControlSystem.WPF.Services;
using QualityControlSystem.WPF.Services.Interfaces;
using QualityControlSystem.WPF.Services.Navigation;
using QualityControlSystem.WPF.ViewModels;
using QualityControlSystem.WPF.Views;
using System;
using System.Windows;

namespace QualityControlSystem.WPF
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        private bool _isHandlingFatalException;

        public App()
        {
            try
            {
                AppHost = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        config.AddEnvironmentVariables();
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<IConfiguration>(context.Configuration);

                        var connectionString = context.Configuration["Database:ConnectionString"] ?? string.Empty;
                        services.AddDbContext<AppDbContext>(options =>
                            options.UseNpgsql(connectionString));

                        //Interfaces
                        services.AddScoped<IUserRepository, UserRepository>();
                        services.AddSingleton<IApiClientService, ApiClientService>();
                        services.AddSingleton<IEdgeDeviceService, EdgeDeviceService>();
                        services.AddSingleton<IDialogService, DialogService>();
                        services.AddSingleton<INotificationService, NotificationService>();
                        services.AddSingleton<IAuthService, AuthService>();
                        services.AddSingleton<NavigationStore>();
                        services.AddSingleton<INavigationService, NavigationService>();
                        services.AddScoped<IUserManagementService, UserManagementService>();
                        services.AddScoped<IQualityTestService, QualityTestService>();

                        //ViewModel
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<LoginViewModel>();
                        services.AddTransient<DashboardViewModel>();
                        services.AddTransient<ProfileViewModel>();
                        services.AddTransient<UserManagementViewModel>();
                        services.AddTransient<OperatorControlViewModel>();
                        services.AddTransient<EquipmentManagementViewModel>();
                        services.AddTransient<TemplatesViewModel>();
                        services.AddTransient<FrameCardsViewModel>();
                        services.AddTransient<QualityTestsViewModel>();

                        //View
                        services.AddSingleton<MainWindow>();
                    })
                    .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания хоста: {ex.Message}\n{ex.StackTrace}");
                Environment.Exit(1);
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Unhandled: {ex?.Message}\n{ex?.StackTrace}",
                                "Фатальная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

            };

            DispatcherUnhandledException += (sender, args) =>
            {
                if (_isHandlingFatalException)
                {
                    args.Handled = false;
                    Shutdown(1);
                    return;
                }

                _isHandlingFatalException = true;
                MessageBox.Show($"Ошибка: {args.Exception.Message}\n{args.Exception.StackTrace}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = false;
                Shutdown(1);
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                if (AppHost == null)
                {
                    Console.WriteLine("AppHost не инициализирован. Проверьте конструктор App().", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }

                await AppHost.StartAsync();

                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                if (mainWindow == null)
                {
                    Console.WriteLine("MainWindow не зарегистрирован в DI.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var navigationService = AppHost.Services.GetRequiredService<INavigationService>();
                if (navigationService == null)
                {
                    Console.WriteLine("INavigationService не зарегистрирован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                mainWindow.Show();

                navigationService.NavigateTo<LoginViewModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение в OnStartup: {ex.Message}\n{ex.StackTrace}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
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
