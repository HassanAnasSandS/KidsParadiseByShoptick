using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KidsParadiseByShoptick.Application.DTOs;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KidsParadiseByShoptick.Application.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AdminAuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<AdminLoginResponse?> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.AdminUsers.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null
            || !string.Equals(user.Username, request.Username, StringComparison.Ordinal)
            || !string.Equals(user.Password, request.Password, StringComparison.Ordinal))
            return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var sessionHours = int.TryParse(_configuration["Jwt:SessionHours"], out var sh) ? sh : 8;
        var rememberDays = int.TryParse(_configuration["Jwt:RememberMeDays"], out var rd) ? rd : 30;
        var expires = request.RememberMe
            ? DateTime.UtcNow.AddDays(rememberDays)
            : DateTime.UtcNow.AddHours(sessionHours);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: [new Claim(ClaimTypes.Name, user.Username), new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
            expires: expires,
            signingCredentials: creds);

        return new AdminLoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username);
    }
}
