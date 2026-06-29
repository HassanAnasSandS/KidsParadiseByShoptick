using KidsParadiseByShoptick.Domain.Models;

namespace KidsParadiseByShoptick.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStats> GetStatsAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
}
