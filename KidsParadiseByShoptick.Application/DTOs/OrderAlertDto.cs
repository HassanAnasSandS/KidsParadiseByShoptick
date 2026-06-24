namespace KidsParadiseByShoptick.Application.DTOs;

public record OrderAlertDto(
    string Id,
    string OrderNumber,
    string Title,
    string Body,
    string CustomerName,
    decimal Total,
    DateTimeOffset ReceivedAt);
