using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QualityControlSystem.WPF.Models;

namespace QualityControlSystem.WPF.Services.Interfaces;

public interface IAuthService
{
    Task<bool> LoginAsync(string login, string password);
    void Logout();
    UserProfileDto? CurrentUser { get; }
    bool IsAuthenticated { get; }
}