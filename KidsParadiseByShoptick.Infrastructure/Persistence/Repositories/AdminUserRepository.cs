using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class AdminUserRepository : Repository<AdminUser>, IAdminUserRepository
{
    public AdminUserRepository(AppDbContext context) : base(context) { }

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            x => EF.Functions.Collate(x.Username, "SQL_Latin1_General_CP1_CS_AS") == username,
            cancellationToken);
}
