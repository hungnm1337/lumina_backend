namespace DataLayer.DTOs.Auth;


public sealed class GoogleUserInfo
{
    
    public string Subject { get; set; } = string.Empty;

    
    public string Email { get; set; } = string.Empty;

    
    public string? Name { get; set; }

    
    public long? ExpirationTimeSeconds { get; set; }
}
