namespace KidsParadiseByShoptick.Application.Interfaces;

public record NewOrderNotification(
    string OrderNumber,
    string CustomerName,
    string Whatsapp,
    string City,
    string Address,
    IReadOnlyList<NewOrderItemLine> Items,
    decimal SubTotal,
    decimal DeliveryCharge,
    decimal Total);

public record NewOrderItemLine(string ToyName, decimal Price);

public interface IOrderNotificationService
{
    Task NotifyNewOrderAsync(NewOrderNotification notification, CancellationToken cancellationToken = default);
}
