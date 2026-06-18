using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Order?> TrackOrderAsync(string email, string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<bool> HasDeliveredOrderForToyAsync(int customerId, int toyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetDeliveredOrdersForCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> IsToyInActiveOrderExcludingAsync(int toyId, int excludeOrderId, CancellationToken cancellationToken = default);
}
