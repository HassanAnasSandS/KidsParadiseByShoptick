using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Interfaces;
using KidsParadiseByShoptick.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace KidsParadiseByShoptick.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context) => _context = context;

    public async Task<DashboardStats> GetStatsAsync(
        DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        var toys = _context.Toys.AsNoTracking();

        var totalToys = await toys.CountAsync(cancellationToken);
        var totalAvailable = await toys.CountAsync(x => !x.IsSold, cancellationToken);
        var totalSold = await toys.CountAsync(x => x.IsSold, cancellationToken);
        var onSale = await toys.CountAsync(x => x.SalePrice != null && x.SalePrice < x.Price, cancellationToken);
        var onRegular = await toys.CountAsync(x => x.SalePrice == null || x.SalePrice >= x.Price, cancellationToken);
        var onSaleAmount = await toys
            .Where(x => x.SalePrice != null && x.SalePrice < x.Price)
            .SumAsync(x => x.SalePrice ?? x.Price, cancellationToken);
        var onRegularAmount = await toys
            .Where(x => x.SalePrice == null || x.SalePrice >= x.Price)
            .SumAsync(x => x.SalePrice ?? x.Price, cancellationToken);
        var allToysAmount = await toys.SumAsync(x => x.SalePrice ?? x.Price, cancellationToken);
        var availableToysAmount = await toys
            .Where(x => !x.IsSold)
            .SumAsync(x => x.SalePrice ?? x.Price, cancellationToken);
        var soldToysAmount = await toys
            .Where(x => x.IsSold)
            .SumAsync(x => x.SalePrice ?? x.Price, cancellationToken);

        var hasDateFilter = dateFrom.HasValue || dateTo.HasValue;
        var from = dateFrom?.Date;
        var toExclusive = dateTo?.Date.AddDays(1);

        int totalCustomers;
        int deliveredOrders;
        decimal deliveredTotal;

        if (hasDateFilter)
        {
            var ordersInRange = ApplyDateFilter(_context.Orders.AsNoTracking(), from, toExclusive);

            totalCustomers = await ordersInRange
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);

            var delivered = ordersInRange.Where(x => x.Status == OrderStatus.Delivered);
            deliveredOrders = await delivered.CountAsync(cancellationToken);
            deliveredTotal = await delivered.SumAsync(x => x.Total, cancellationToken);
        }
        else
        {
            totalCustomers = await _context.Customers.AsNoTracking().CountAsync(cancellationToken);

            var delivered = _context.Orders.AsNoTracking()
                .Where(x => x.Status == OrderStatus.Delivered);
            deliveredOrders = await delivered.CountAsync(cancellationToken);
            deliveredTotal = await delivered.SumAsync(x => x.Total, cancellationToken);
        }

        return new DashboardStats
        {
            TotalToys = totalToys,
            TotalAvailableToys = totalAvailable,
            TotalSoldToys = totalSold,
            TotalToysOnSale = onSale,
            TotalToysOnRegular = onRegular,
            RegularToysTotalAmount = onRegularAmount,
            OnSaleToysTotalAmount = onSaleAmount,
            AllToysTotalAmount = allToysAmount,
            AvailableToysTotalAmount = availableToysAmount,
            AllSoldToysTotalAmount = soldToysAmount,
            TotalCustomers = totalCustomers,
            TotalDeliveredOrders = deliveredOrders,
            AllDeliveredOrdersTotalAmount = deliveredTotal,
        };
    }

    private static IQueryable<Domain.Entities.Order> ApplyDateFilter(
        IQueryable<Domain.Entities.Order> query, DateTime? from, DateTime? toExclusive)
    {
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (toExclusive.HasValue)
            query = query.Where(x => x.CreatedAt < toExclusive.Value);

        return query;
    }
}
