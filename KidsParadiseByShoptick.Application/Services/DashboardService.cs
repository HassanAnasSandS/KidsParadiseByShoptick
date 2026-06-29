using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository) =>
        _dashboardRepository = dashboardRepository;

    public async Task<AdminDashboardDto> GetAdminStatsAsync(
        DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardRepository.GetStatsAsync(dateFrom, dateTo, cancellationToken);
        return new AdminDashboardDto(
            stats.TotalToys,
            stats.TotalAvailableToys,
            stats.TotalSoldToys,
            stats.TotalToysOnSale,
            stats.TotalToysOnRegular,
            stats.RegularToysTotalAmount,
            stats.OnSaleToysTotalAmount,
            stats.AllToysTotalAmount,
            stats.AvailableToysTotalAmount,
            stats.AllSoldToysTotalAmount,
            stats.TotalCustomers,
            stats.TotalDeliveredOrders,
            stats.AllDeliveredOrdersTotalAmount);
    }
}
