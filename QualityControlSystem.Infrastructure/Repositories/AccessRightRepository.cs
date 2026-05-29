using QualityControlSystem.Infrastructure.Entities;
using QualityControlSystem.Infrastructure.Repositories.Interfaces;

namespace QualityControlSystem.Infrastructure.Repositories;

public class AccessRightRepository : Repository<AccessRight>, IAccessRightRepository
{
    public AccessRightRepository(AppDbContext context) : base(context) { }
    // Additional query methods can be added here
}
