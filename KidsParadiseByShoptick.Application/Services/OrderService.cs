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

    public OrderService(IUnitOfWork unitOfWork, IDeliveryChargeService deliveryCharge, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _deliveryCharge = deliveryCharge;
        _fileStorage = fileStorage;
    }

    public async Task<OrderPlacedDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToyIds.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var uniqueToyIds = request.ToyIds.Distinct().ToList();
        if (uniqueToyIds.Count != request.ToyIds.Count)
            throw new InvalidOperationException("Each toy can only be ordered once.");

        var email = request.Email.Trim().ToLower();
        var customer = await _unitOfWork.Customers.GetByEmailAsync(email, cancellationToken);

        if (customer is null)
        {
            customer = new Customer
            {
                Email = email,
                Name = request.Name.Trim(),
                Phone = request.Phone.Trim(),
                Whatsapp = request.Whatsapp.Trim(),
                City = request.City.Trim(),
                Address = request.Address.Trim()
            };
            await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            customer.Name = request.Name.Trim();
            customer.Phone = request.Phone.Trim();
            customer.Whatsapp = request.Whatsapp.Trim();
            customer.City = request.City.Trim();
            customer.Address = request.Address.Trim();
            await _unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        }

        var orderItems = new List<OrderItem>();
        decimal subTotal = 0;

        foreach (var toyId in uniqueToyIds)
        {
            var toy = await _unitOfWork.Toys.GetByIdAsync(toyId, cancellationToken)
                ?? throw new InvalidOperationException($"Toy {toyId} not found.");

            if (toy.IsSold)
                throw new InvalidOperationException($"Toy '{toy.Name}' is already sold.");

            subTotal += toy.EffectivePrice;
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
            Phone = request.Phone.Trim(),
            Whatsapp = request.Whatsapp.Trim(),
            Items = orderItems
        };

        await _unitOfWork.Orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderPlacedDto(order.OrderNumber, order.Total, order.DeliveryCharge);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return [];

        var orders = await _unitOfWork.Orders.GetByCustomerEmailAsync(email.Trim(), cancellationToken);
        return orders.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Orders.GetAllWithDetailsAsync(cancellationToken);
        return orders.Select(Map).ToList();
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

        if (orderStatus == OrderStatus.Confirmed || request.AdvanceAmount.HasValue)
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
        }

        if (orderStatus == OrderStatus.Cancelled || orderStatus == OrderStatus.Pending)
            order.AdvanceAmount = null;

        if (orderStatus == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
            await SetToysSoldStateForOrderAsync(order, sold: false, cancellationToken);
        else if (previousStatus == OrderStatus.Cancelled && orderStatus != OrderStatus.Cancelled)
            await SetToysSoldStateForOrderAsync(order, sold: true, cancellationToken);

        await _unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(order);
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
        return new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.SubTotal,
            order.DeliveryCharge,
            order.Total,
            order.AdvanceAmount,
            order.Total - advance,
            order.City,
            order.Address,
            order.Phone,
            order.Whatsapp,
            order.TrackingNumber,
            order.Customer.Name,
            order.Customer.Email,
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
