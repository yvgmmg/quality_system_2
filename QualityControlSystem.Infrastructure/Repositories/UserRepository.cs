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
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.PersonnelNumber == personnelNumber);
    }

    public async Task<UserProfile?> ValidateCredentialsAsync(string personnelNumber, string password)
    {
        var user = await GetByPersonnelNumberAsync(personnelNumber);
        if (user == null)
            return null;

        bool isValid;
        try
        {
            isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
        }
        catch
        {
            isValid = false;
        }

        return isValid ? user : null;
    }

    public async Task AddAsync(UserProfile user)
    {
        await _context.UserProfiles.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<UserProfile?> GetByIdAsync(int id)
    {
        return await _context.UserProfiles
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserProfileId == id);
    }

    public async Task<IEnumerable<UserProfile>> GetAllAsync()
    {
        return await _context.UserProfiles
            .Include(u => u.Role)
            .ToListAsync();
    }

    public async Task UpdateAsync(UserProfile user)
    {
        _context.UserProfiles.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.UserProfiles.FindAsync(id);
        if (entity != null)
        {
            _context.UserProfiles.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
