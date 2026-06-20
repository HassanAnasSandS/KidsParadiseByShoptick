namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    IToyCategoryRepository Categories { get; }
    IToyRepository Toys { get; }
    IOrderRepository Orders { get; }
    IReviewRepository Reviews { get; }
    IAdminUserRepository AdminUsers { get; }
    ISiteImageRepository SiteImages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
