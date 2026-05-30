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
    Task<UserProfile?> ValidateCredentialsAsync(string personnelNumber, string password);
    Task AddAsync(UserProfile user);
    Task<UserProfile?> GetByIdAsync(int id);
    Task<IEnumerable<UserProfile>> GetAllAsync();
    Task UpdateAsync(UserProfile user);
    Task DeleteAsync(int id);
}