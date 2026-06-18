namespace KidsParadiseByShoptick.Domain.Entities;

public class AdminUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
