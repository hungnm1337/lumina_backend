using DataLayer.DTOs.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Auth;


public interface IGoogleAuthService
{
    
    Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken);
}


public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string? _googleClientId;

    public GoogleAuthService(
        ILogger<GoogleAuthService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _googleClientId = configuration["Google:ClientId"];
    }

    public async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(_googleClientId))
        {
            _logger.LogError("Google authentication attempted but ClientId is not configured");
            throw GoogleAuthException.ConfigurationError("Google login is not configured");
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw GoogleAuthException.InvalidToken("Google token is required");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleClientId }
                });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google token received");
            throw GoogleAuthException.InvalidToken("Invalid Google token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Google token");
            throw GoogleAuthException.ValidationFailed("Failed to verify Google token");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            _logger.LogWarning("Google token validated but email is missing. Subject: {Subject}", payload.Subject);
            throw GoogleAuthException.InvalidToken("Google account email is required");
        }

        var userInfo = new GoogleUserInfo
        {
            Subject = payload.Subject,
            Email = payload.Email,
            Name = payload.Name,
            ExpirationTimeSeconds = payload.ExpirationTimeSeconds
        };

        _logger.LogInformation("Successfully validated Google token for email: {Email}", payload.Email);

        return userInfo;
    }
}


public sealed class GoogleAuthException : Exception
{
    public GoogleAuthException(string message) : base(message)
    {
    }

    public GoogleAuthException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static GoogleAuthException ConfigurationError(string message)
        => new(message);

    public static GoogleAuthException InvalidToken(string message)
        => new(message);

    public static GoogleAuthException ValidationFailed(string message)
        => new(message);
}
