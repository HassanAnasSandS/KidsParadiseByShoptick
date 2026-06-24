using KidsParadiseByShoptick.Domain.Entities;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerWhatsappPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByCustomerWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAdminPagedAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAdminAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default);
    Task<bool> HasDeliveredOrderForToyAsync(int customerId, int toyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetDeliveredOrdersForCustomerAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> IsToyInActiveOrderExcludingAsync(int toyId, int excludeOrderId, CancellationToken cancellationToken = default);
}
