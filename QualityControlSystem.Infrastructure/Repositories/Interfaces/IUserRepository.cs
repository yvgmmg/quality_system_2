using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QualityControlSystem.Infrastructure.Entities;

namespace QualityControlSystem.Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    Task<UserProfile?> GetByPersonnelNumberAsync(string personnelNumber);
    Task<bool> ValidateCredentialsAsync(string login, string password);
}