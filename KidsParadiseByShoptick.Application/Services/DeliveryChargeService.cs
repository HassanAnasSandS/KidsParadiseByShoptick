namespace KidsParadiseByShoptick.Application.Services;

public class DeliveryChargeService : Interfaces.IDeliveryChargeService
{
    public decimal Calculate(string city)
        => city.Trim().Equals("karachi", StringComparison.OrdinalIgnoreCase) ? 300m : 400m;
}
