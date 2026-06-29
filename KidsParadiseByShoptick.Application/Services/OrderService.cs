using KidsParadiseByShoptick.Application;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Enums;
using KidsParadiseByShoptick.Domain.Interfaces;

namespace KidsParadiseByShoptick.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeliveryChargeService _deliveryCharge;
    private readonly IFileStorageService _fileStorage;
    private readonly IOrderNotificationService _orderNotification;

    public OrderService(
        IUnitOfWork unitOfWork,
        IDeliveryChargeService deliveryCharge,
        IFileStorageService fileStorage,
        IOrderNotificationService orderNotification)
    {
        _unitOfWork = unitOfWork;
        _deliveryCharge = deliveryCharge;
        _fileStorage = fileStorage;
        _orderNotification = orderNotification;
    }

    public async Task<OrderPlacedDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToyIds.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var uniqueToyIds = request.ToyIds.Distinct().ToList();
        if (uniqueToyIds.Count != request.ToyIds.Count)
            throw new InvalidOperationException("Each toy can only be ordered once.");

        var whatsapp = ContactNormalizer.NormalizeWhatsapp(request.Whatsapp);
        if (string.IsNullOrWhiteSpace(whatsapp))
            throw new InvalidOperationException("WhatsApp number is required.");

        var customer = await _unitOfWork.Customers.GetByWhatsappAsync(whatsapp, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Email = string.Empty,
                Name = request.Name.Trim(),
                Phone = string.Empty,
                Whatsapp = whatsapp,
                City = request.City.Trim(),
                Address = request.Address.Trim()
            };
            await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            customer.Name = request.Name.Trim();
            customer.Whatsapp = whatsapp;
            customer.City = request.City.Trim();
            customer.Address = request.Address.Trim();
            await _unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        }

        var orderItems = new List<OrderItem>();
        var itemLines = new List<NewOrderItemLine>();
        decimal subTotal = 0;

        foreach (var toyId in uniqueToyIds)
        {
            var toy = await _unitOfWork.Toys.GetByIdAsync(toyId, cancellationToken)
                ?? throw new InvalidOperationException($"Toy {toyId} not found.");

            if (toy.IsSold)
                throw new InvalidOperationException($"Toy '{toy.Name}' is already sold.");

            subTotal += toy.EffectivePrice;
            itemLines.Add(new NewOrderItemLine(toy.Name, toy.EffectivePrice));
            toy.IsSold = true;
            await _unitOfWork.Toys.UpdateAsync(toy, cancellationToken);

            orderItems.Add(new OrderItem { ToyId = toy.Id });
        }

        var deliveryCharge = _deliveryCharge.Calculate(request.City);
        var order = new Order
        {
            CustomerId = customer.Id,
            OrderNumber = GenerateOrderNumber(),
            SubTotal = subTotal,
            DeliveryCharge = deliveryCharge,
            Total = subTotal + deliveryCharge,
            Status = OrderStatus.Pending,
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = string.Empty,
            Whatsapp = whatsapp,
            Items = orderItems
        };

        await _unitOfWork.Orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _orderNotification.NotifyNewOrderAsync(new NewOrderNotification(
            order.OrderNumber,
            customer.Name,
            whatsapp,
            order.City,
            order.Address,
            itemLines,
            order.SubTotal,
            order.DeliveryCharge,
            order.Total), cancellationToken);

        return new OrderPlacedDto(order.OrderNumber, order.Total, order.DeliveryCharge);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersByWhatsappAsync(string whatsapp, CancellationToken cancellationToken = default)
    {
        var result = await GetOrdersByWhatsappPagedAsync(whatsapp, 1, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersByWhatsappPagedAsync(
        string whatsapp, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(whatsapp))
            return new PagedResult<OrderDto>([], 0, page, pageSize);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var orders = await _unitOfWork.Orders.GetByCustomerWhatsappPagedAsync(whatsapp, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Orders.CountByCustomerWhatsappAsync(whatsapp, cancellationToken);
        return new PagedResult<OrderDto>(orders.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAdminPagedAsync(null, null, null, null, null, "newest", 1, int.MaxValue, cancellationToken);
        return result.Items;
    }

    public async Task<PagedResult<OrderDto>> GetAdminPagedAsync(
        string? status, string? search, string? city, DateTime? dateFrom, DateTime? dateTo, string? sort,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var orders = await _unitOfWork.Orders.GetAdminPagedAsync(status, search, city, dateFrom, dateTo, sort, page, pageSize, cancellationToken);
        var total = await _unitOfWork.Orders.CountAdminAsync(status, search, city, dateFrom, dateTo, cancellationToken);
        return new PagedResult<OrderDto>(orders.Select(Map).ToList(), total, page, pageSize);
    }

    public Task<IReadOnlyList<string>> GetAdminCitiesAsync(CancellationToken cancellationToken = default)
        => _unitOfWork.Orders.GetDistinctCitiesAsync(cancellationToken);

    public async Task<OrderStatusCountsDto> GetAdminStatusCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _unitOfWork.Orders.GetStatusCountsAsync(cancellationToken);
        return new OrderStatusCountsDto(
            counts.Total, counts.Pending, counts.Confirmed, counts.Shipped, counts.Delivered, counts.Cancelled);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var orderStatus))
            throw new InvalidOperationException("Invalid order status.");

        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id, cancellationToken);
        if (order is null) return null;

        var previousStatus = order.Status;
        order.Status = orderStatus;
        if (orderStatus == OrderStatus.Shipped && !string.IsNullOrWhiteSpace(request.TrackingNumber))
            order.TrackingNumber = request.TrackingNumber.Trim();

        if (orderStatus == OrderStatus.Confirmed || request.AdvanceAmount.HasValue || request.DiscountAmount.HasValue)
        {
            if (request.AdvanceAmount.HasValue)
            {
                if (request.AdvanceAmount.Value < 0 || request.AdvanceAmount.Value > order.Total)
                    throw new InvalidOperationException("Advance amount must be between 0 and order total.");
                order.AdvanceAmount = request.AdvanceAmount.Value;
            }
            else if (orderStatus == OrderStatus.Confirmed && previousStatus != OrderStatus.Confirmed)
            {
                order.AdvanceAmount ??= 0;
            }

            if (request.DiscountAmount.HasValue)
            {
                if (request.DiscountAmount.Value < 0 || request.DiscountAmount.Value > order.Total)
                    throw new InvalidOperationException("Discount amount must be between 0 and order total.");
                order.DiscountAmount = request.DiscountAmount.Value;
            }
            else if (orderStatus == OrderStatus.Confirmed && previousStatus != OrderStatus.Confirmed)
            {
                order.DiscountAmount ??= 0;
            }

            var advance = order.AdvanceAmount ?? 0;
            var discount = order.DiscountAmount ?? 0;
            if (advance + discount > order.Total)
                throw new InvalidOperationException("Advance and discount combined cannot exceed order total.");
        }

        if (orderStatus == OrderStatus.Cancelled || orderStatus == OrderStatus.Pending)
        {
            order.AdvanceAmount = null;
            order.DiscountAmount = null;
        }

        if (orderStatus == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
            await SetToysSoldStateForOrderAsync(order, sold: false, cancellationToken);
        else if (previousStatus == OrderStatus.Cancelled && orderStatus != OrderStatus.Cancelled)
            await SetToysSoldStateForOrderAsync(order, sold: true, cancellationToken);

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(order);
    }

    public async Task<OrderDto?> GetByIdAdminAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task<OrderDto?> UpdateAdminAsync(int id, AdminUpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToyIds.Count == 0)
            throw new InvalidOperationException("Order must have at least one toy.");

        var uniqueToyIds = request.ToyIds.Distinct().ToList();
        if (uniqueToyIds.Count != request.ToyIds.Count)
            throw new InvalidOperationException("Each toy can only appear once.");

        if (request.DeliveryCharge < 0)
            throw new InvalidOperationException("Delivery charge cannot be negative.");

        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cancelled orders cannot be edited.");

        var whatsapp = ContactNormalizer.NormalizeWhatsapp(request.Whatsapp);
        if (string.IsNullOrWhiteSpace(whatsapp))
            throw new InvalidOperationException("WhatsApp number is required.");

        var currentToyIds = order.Items.Select(i => i.ToyId).ToHashSet();
        var newToyIds = uniqueToyIds.ToHashSet();
        var toysChanged = !currentToyIds.SetEquals(newToyIds);

        if (toysChanged && order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot change toys on a delivered order.");

        if (toysChanged && order.Status != OrderStatus.Cancelled)
        {
            var toRemove = currentToyIds.Except(newToyIds).ToList();
            var toAdd = newToyIds.Except(currentToyIds).ToList();

            foreach (var toyId in toRemove)
            {
                var item = order.Items.First(i => i.ToyId == toyId);
                order.Items.Remove(item);

                var toy = item.Toy ?? await _unitOfWork.Toys.GetByIdAsync(toyId, cancellationToken);
                if (toy is not null)
                {
                    toy.IsSold = false;
                    await _unitOfWork.Toys.UpdateAsync(toy, cancellationToken);
                }
            }

            foreach (var toyId in toAdd)
            {
                var toy = await _unitOfWork.Toys.GetByIdAsync(toyId, cancellationToken)
                    ?? throw new InvalidOperationException($"Toy {toyId} not found.");

                if (toy.IsSold)
                {
                    var inOtherOrder = await _unitOfWork.Orders.IsToyInActiveOrderExcludingAsync(toyId, order.Id, cancellationToken);
                    if (inOtherOrder)
                        throw new InvalidOperationException($"Toy '{toy.Name}' is already in another active order.");
                    throw new InvalidOperationException($"Toy '{toy.Name}' is already sold.");
                }

                toy.IsSold = true;
                await _unitOfWork.Toys.UpdateAsync(toy, cancellationToken);
                order.Items.Add(new OrderItem { ToyId = toy.Id, OrderId = order.Id });
            }
        }

        decimal subTotal = 0;
        foreach (var toyId in uniqueToyIds)
        {
            var toy = await _unitOfWork.Toys.GetByIdAsync(toyId, cancellationToken)
                ?? throw new InvalidOperationException($"Toy {toyId} not found.");
            subTotal += toy.EffectivePrice;
        }

        order.SubTotal = subTotal;
        order.DeliveryCharge = request.DeliveryCharge;
        order.Total = subTotal + request.DeliveryCharge;
        order.City = request.City.Trim();
        order.Address = request.Address.Trim();
        order.Whatsapp = whatsapp;
        order.TrackingNumber = request.TrackingNumber is null
            ? order.TrackingNumber
            : (string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber.Trim());

        order.Customer.Name = request.CustomerName.Trim();
        order.Customer.Whatsapp = whatsapp;
        order.Customer.City = request.City.Trim();
        order.Customer.Address = request.Address.Trim();
        await _unitOfWork.Customers.UpdateAsync(order.Customer, cancellationToken);

        if (request.AdvanceAmount.HasValue)
        {
            if (request.AdvanceAmount.Value < 0 || request.AdvanceAmount.Value > order.Total)
                throw new InvalidOperationException("Advance amount must be between 0 and order total.");
            order.AdvanceAmount = request.AdvanceAmount.Value;
        }

        if (request.DiscountAmount.HasValue)
        {
            if (request.DiscountAmount.Value < 0 || request.DiscountAmount.Value > order.Total)
                throw new InvalidOperationException("Discount amount must be between 0 and order total.");
            order.DiscountAmount = request.DiscountAmount.Value;
        }

        var advance = order.AdvanceAmount ?? 0;
        var discount = order.DiscountAmount ?? 0;
        if (advance + discount > order.Total)
            throw new InvalidOperationException("Advance and discount combined cannot exceed order total.");

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.Orders.GetWithDetailsAsync(id, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    private async Task SetToysSoldStateForOrderAsync(Order order, bool sold, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var toy = item.Toy ?? await _unitOfWork.Toys.GetByIdAsync(item.ToyId, cancellationToken);
            if (toy is null) continue;

            if (sold)
            {
                var inOtherOrder = await _unitOfWork.Orders.IsToyInActiveOrderExcludingAsync(item.ToyId, order.Id, cancellationToken);
                if (inOtherOrder)
                    throw new InvalidOperationException($"Toy '{toy.Name}' is already reserved in another active order.");
            }

            toy.IsSold = sold;
            await _unitOfWork.Toys.UpdateAsync(toy, cancellationToken);
        }
    }

    private static string GenerateOrderNumber() =>
        $"KP{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";

    private OrderDto Map(Order order)
    {
        var advance = order.AdvanceAmount ?? 0;
        var discount = order.DiscountAmount ?? 0;
        return new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.SubTotal,
            order.DeliveryCharge,
            order.Total,
            order.AdvanceAmount,
            order.DiscountAmount,
            order.Total - discount - advance,
            order.City,
            order.Address,
            order.Whatsapp,
            order.TrackingNumber,
            order.Customer.Name,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemDto(
                i.ToyId,
                i.Toy?.Name ?? "",
                i.Toy?.EffectivePrice ?? 0,
                GetToyImage(i.Toy)
            )).ToList());
    }

    private string? GetToyImage(Toy? toy)
    {
        if (toy?.Images is null || toy.Images.Count == 0) return null;
        var path = toy.Images.OrderBy(img => img.SortOrder).First().ImagePath;
        return _fileStorage.GetPublicUrl(path);
    }
}
