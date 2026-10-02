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

        // Affiliate stats are informational only — never deducted from sales/revenue above.
        var affiliate = await GetAffiliateStatsAsync(from, toExclusive, cancellationToken);

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
            TotalAffiliatePartners = affiliate.TotalPartners,
            ActiveAffiliatePartners = affiliate.ActivePartners,
            AffiliatedOrdersCount = affiliate.AffiliatedOrders,
            AffiliateCommissionTotal = affiliate.CommissionTotal,
            AffiliatePaidTotal = affiliate.PaidTotal,
            AffiliateOutstandingTotal = affiliate.OutstandingTotal,
            AffiliatePartners = affiliate.Partners,
        };
    }

    private async Task<(
        int TotalPartners,
        int ActivePartners,
        int AffiliatedOrders,
        decimal CommissionTotal,
        decimal PaidTotal,
        decimal OutstandingTotal,
        IReadOnlyList<DashboardAffiliatePartnerStat> Partners)> GetAffiliateStatsAsync(
        DateTime? from, DateTime? toExclusive, CancellationToken cancellationToken)
    {
        var partners = await _context.AffiliatePartners.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var totalPartners = partners.Count;
        var activePartners = partners.Count(x => x.IsActive);

        var ordersQuery = _context.Orders.AsNoTracking()
            .Where(x => x.AffiliatePartnerId != null);
        ordersQuery = ApplyDateFilter(ordersQuery, from, toExclusive);
        var affiliatedOrders = await ordersQuery.CountAsync(cancellationToken);

        var orderCounts = await ordersQuery
            .GroupBy(x => x.AffiliatePartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var orderCountMap = orderCounts.ToDictionary(x => x.PartnerId, x => x.Count);

        var ledgerQuery = _context.AffiliateLedgerEntries.AsNoTracking().AsQueryable();
        if (from.HasValue)
            ledgerQuery = ledgerQuery.Where(x => x.CreatedAt >= from.Value);
        if (toExclusive.HasValue)
            ledgerQuery = ledgerQuery.Where(x => x.CreatedAt < toExclusive.Value);

        var ledgerAgg = await ledgerQuery
            .GroupBy(x => new { x.AffiliatePartnerId, x.Type })
            .Select(g => new
            {
                g.Key.AffiliatePartnerId,
                g.Key.Type,
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync(cancellationToken);

        decimal totalCommission = 0;
        decimal totalPaid = 0;
        var partnerStats = new List<DashboardAffiliatePartnerStat>();

        foreach (var partner in partners)
        {
            var commission = ledgerAgg
                .Where(x => x.AffiliatePartnerId == partner.Id && x.Type == AffiliateLedgerEntryType.Commission)
                .Sum(x => x.Amount);
            var paid = ledgerAgg
                .Where(x => x.AffiliatePartnerId == partner.Id && x.Type == AffiliateLedgerEntryType.Payment)
                .Sum(x => x.Amount);
            var reversed = ledgerAgg
                .Where(x => x.AffiliatePartnerId == partner.Id && x.Type == AffiliateLedgerEntryType.Reversal)
                .Sum(x => x.Amount);

            var netCommission = decimal.Round(commission - reversed, 2);
            var paidRounded = decimal.Round(paid, 2);
            var balance = decimal.Round(netCommission - paidRounded, 2);
            var attributed = orderCountMap.GetValueOrDefault(partner.Id);

            var hasDateFilter = from is not null || toExclusive is not null;
            if (hasDateFilter && attributed == 0 && netCommission == 0 && paidRounded == 0)
                continue;

            totalCommission += netCommission;
            totalPaid += paidRounded;

            partnerStats.Add(new DashboardAffiliatePartnerStat
            {
                Id = partner.Id,
                Name = partner.Name,
                Code = partner.Code,
                IsActive = partner.IsActive,
                AttributedOrders = attributed,
                TotalCommission = netCommission,
                TotalPaid = paidRounded,
                Balance = balance,
            });
        }

        var outstanding = decimal.Round(totalCommission - totalPaid, 2);
        return (
            totalPartners,
            activePartners,
            affiliatedOrders,
            decimal.Round(totalCommission, 2),
            decimal.Round(totalPaid, 2),
            outstanding,
            partnerStats.OrderByDescending(x => x.TotalCommission).ThenBy(x => x.Name).ToList());
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
