using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataLayer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ServiceLayer.Auth;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        
        var jwtSection = configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT secret key is not configured.");
        _issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        _audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        _accessTokenExpirationMinutes = Math.Clamp(jwtSection.GetValue("AccessTokenExpirationMinutes", 60), 1, 1440);

        // Validate key size for HS256 (requires >= 256 bits = 32 bytes)
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 256 bits (32 bytes). Update configuration at 'Jwt:SecretKey'.");
        }

        _signingKey = new SymmetricSecurityKey(keyBytes);
    }

    public JwtTokenResult GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new JwtTokenResult(token, (int)TimeSpan.FromMinutes(_accessTokenExpirationMinutes).TotalSeconds, expiresAtUtc);
    }
}

public sealed record JwtTokenResult(string Token, int ExpiresInSeconds, DateTime ExpiresAtUtc);