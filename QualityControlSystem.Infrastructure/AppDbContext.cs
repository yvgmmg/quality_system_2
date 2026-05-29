using Microsoft.EntityFrameworkCore;
using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.Infrastructure.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace QualityControlSystem.Infrastructure;

public class AppDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Зададим enum в виде строки
        modelBuilder.Entity<UserProfile>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // Заполним демо-пользователями (хеши паролей позже)
        modelBuilder.Entity<UserProfile>().HasData(
            new UserProfile
            {
                UserProfileId = 1,
                Name = "Админ",
                Surname = "Админов",
                Patron = null,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                PersonnelNumber = "A000001",
                Role = UserRole.Admin,
                WorkshopId = 0
            },
            new UserProfile
            {
                UserProfileId = 2,
                Name = "Иван",
                Surname = "Петров",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("operator"),
                PersonnelNumber = "O123456",
                Role = UserRole.Operator,
                WorkshopId = 1
            },
            new UserProfile
            {
                UserProfileId = 3,
                Name = "Сергей",
                Surname = "Сидоров",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("equipment"),
                PersonnelNumber = "E789012",
                Role = UserRole.EquipmentSpecialist,
                WorkshopId = 1
            },
            new UserProfile
            {
                UserProfileId = 4,
                Name = "Мария",
                Surname = "Иванова",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("qcofficer"),
                PersonnelNumber = "Q345678",
                Role = UserRole.QualityControlOfficer,
                WorkshopId = 1
            }
        );
    }
}
