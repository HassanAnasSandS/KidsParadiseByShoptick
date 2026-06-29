using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminStatsAsync(
        DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
}
