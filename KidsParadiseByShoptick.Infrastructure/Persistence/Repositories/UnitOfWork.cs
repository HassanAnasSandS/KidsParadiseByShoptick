using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        ICustomerRepository customers,
        IToyCategoryRepository categories,
        IToyRepository toys,
        IOrderRepository orders,
        IReviewRepository reviews,
        IAdminUserRepository adminUsers,
        ISiteImageRepository siteImages)
    {
        _context = context;
        Customers = customers;
        Categories = categories;
        Toys = toys;
        Orders = orders;
        Reviews = reviews;
        AdminUsers = adminUsers;
        SiteImages = siteImages;
    }

    public ICustomerRepository Customers { get; }
    public IToyCategoryRepository Categories { get; }
    public IToyRepository Toys { get; }
    public IOrderRepository Orders { get; }
    public IReviewRepository Reviews { get; }
    public IAdminUserRepository AdminUsers { get; }
    public ISiteImageRepository SiteImages { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
