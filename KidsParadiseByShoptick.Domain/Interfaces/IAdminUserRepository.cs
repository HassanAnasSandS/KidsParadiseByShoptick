using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IAdminUserRepository : IRepository<AdminUser>
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
