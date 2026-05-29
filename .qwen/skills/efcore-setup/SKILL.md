---
name: efcore-setup
description: Генерация EF Core сущностей, DbContext и репозиториев из SQL‑скриптов
source: auto-skill
extracted_at: '2026-05-29T12:38:00.000Z'
---

## Цель
Автоматически создавать модели C# для PostgreSQL‑базы, полностью настраивать `DbContext` (DbSet, Fluent API, конверсии Enum → string, каскадное удаление, индексы) и базовый репозиторный слой (общий `IRepository<T>`/`Repository<T>` и специализированные репозитории).

## Шаги
1. **Список сущностей** – собрать имена всех таблиц из SQL‑скрипта.  
2. **Классы‑модели** – для каждой таблицы генерировать C#‑класс:
   * атрибут `[Table("table_name")]`;
   * свойства с типами, соответствующими типам PostgreSQL (`serial4` → `int`, `varchar`/`text` → `string`, `date`/`timestamp` → `DateTime`, `float4` → `float`, `float8` → `double`, `numeric` → `decimal`, `bool` → `bool`).
   * пометка PK атрибутом `[Key]` и `[Column("col_name")]`.
   * для nullable колонок использовать `type?`.
   * добавить навигационные свойства (`virtual`) для всех внешних ключей.
3. **Enum‑модели** – для каждого пользовательского enum из скрипта создать отдельный `enum` в папке `Enums` и в `OnModelCreating` прописать `HasConversion<string>()`.
4. **DbContext**:
   * объявить `DbSet<T>` для **всех** сущностей.
   * в `OnModelCreating`:
     * конвертировать все enum‑свойства в строки.
     * задать отношения (FK → `HasOne(...).WithMany().HasForeignKey(...).OnDelete(...)`).
     * указать каскадные поведения (обычно `Restrict` для справочников, `SetNull` для необязательных связей, `Cascade` для таблиц‑связей).
     * создать уникальные индексы (например, `UserProfile.PersonnelNumber`).
   * добавить первоначальное заполнение (`HasData`) при необходимости.
5. **Репозитории**:
   * общий интерфейс `IRepository<T>` с CRUD‑методами.
   * базовый класс `Repository<T>` реализующий интерфейс, используя `DbContext.Set<T>()`.
   * для каждой бизнес‑сущности (например, `AccessRight`, `UserProfile`, `Batch`) создать специализированный интерфейс (наследует `IRepository<T>`) и конкретный класс, наследующий `Repository<T>`.
6. **Структура проекта** – разместить модели в `Infrastructure/Entities`, enum‑ы в `Infrastructure/Enums`, репозитории в `Infrastructure/Repositories` и их интерфейсы в `Infrastructure/Repositories/Interfaces`.

## Ключевые детали
* **Многие‑ко‑многим** реализуются через отдельные таблицы‑связи (пример `UserProfileAccessRights`). В `OnModelCreating` нужно настроить оба FK и `OnDelete(Cascade)` для связи.
* **Enum → string** важно для PostgreSQL, так как EF Core будет использовать тип `text`.
* **Навигационные свойства** упрощают LINQ‑запросы и позволяют использовать `Include`.
* **Индексы** повышают производительность поиска по часто используемым полям.
* **Seed‑данные** помещаются в `OnModelCreating` через `HasData`.

## Пример кода
```csharp
public class AppDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<AccessRight> AccessRights { get; set; } = null!;
    // … остальные DbSet

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Enum conversion
        modelBuilder.Entity<UserProfile>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // FK example
        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.Workshop)
            .WithMany()
            .HasForeignKey(up => up.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many‑to‑many via link table
        modelBuilder.Entity<UserProfileAccessRights>()
            .HasOne(ua => ua.UserProfile)
            .WithMany(up => up.AccessRights)
            .HasForeignKey(ua => ua.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.PersonnelNumber)
            .IsUnique();
    }
}
```

## Когда использовать
* При начальном этапе разработки, когда нужно быстро построить слой доступа к данным из уже готового SQL‑скрипта.
* При необходимости добавить новые таблицы – достаточно добавить модель и соответствующее объявление в `DbContext`.
* При желании поддерживать единый репозиториный паттерн для всех сущностей.
