using KidsParadiseByShoptick.Application.DTOs;

namespace KidsParadiseByShoptick.Application.Interfaces;

public interface IDeliveryChargeService
{
    decimal Calculate(string city);

    Task<DeliveryChargeSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<DeliveryChargeSettingsDto> UpdateAsync(
        UpdateDeliveryChargeSettingsRequest request,
        CancellationToken cancellationToken = default);
}
