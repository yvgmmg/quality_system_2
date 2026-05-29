using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;

namespace QualityControlSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByPersonnelNumberAsync(string personnelNumber)
    {
        return await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.PersonnelNumber == personnelNumber);
    }

    public async Task<bool> ValidateCredentialsAsync(string login, string password)
    {
        var user = await GetByPersonnelNumberAsync(login);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}
