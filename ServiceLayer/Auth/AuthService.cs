using System.Globalization;
using System.Text;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Auth;


public sealed class AuthService : IAuthService
{

    private const string GoogleProvider = "Google";
    private const int UsernameMaxLength = 20;
    private const int NameMaxLength = 50;



    private readonly LuminaSystemContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ILogger<AuthService> _logger;
    private readonly int _defaultRoleId = 4;



    public AuthService(
        LuminaSystemContext context,
        IJwtTokenService jwtTokenService,
        IGoogleAuthService googleAuthService,
        ILogger<AuthService> logger
        )
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _googleAuthService = googleAuthService;
        _logger = logger;
    }


    public async Task<LoginResponse> LoginAsync(LoginRequestDTO request)
    {
        var identifier = request.Username.Trim();
        var account = await FindAccountByIdentifierAsync(identifier);

        // Guard clauses
        if (account?.User == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed for identifier: {Identifier} - Account not found or no password", identifier);
            throw AuthServiceException.Unauthorized("Invalid username or password");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed for identifier: {Identifier} - Invalid password", identifier);
            throw AuthServiceException.Unauthorized("Invalid username or password");
        }

        if (account.User.IsActive is false)
        {
            _logger.LogWarning("Login attempt for inactive account: {Username}", account.Username);
            throw AuthServiceException.Unauthorized("Account is inactive");
        }

        _logger.LogInformation("User {Username} logged in successfully", account.Username);
        return CreateLoginResponse(account.User, account.Username);
    }

    public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        // Xác thực với Google (đã tách thành service riêng)
        GoogleUserInfo googleUserInfo;
        try
        {
            googleUserInfo = await _googleAuthService.ValidateGoogleTokenAsync(request.Token);
        }
        catch (GoogleAuthException ex)
        {
            _logger.LogWarning(ex, "Google authentication failed");
            throw AuthServiceException.Unauthorized(ex.Message);
        }

        var normalizedEmail = NormalizeEmail(googleUserInfo.Email);
        var account = await FindOrCreateGoogleAccountAsync(googleUserInfo, normalizedEmail, request.Token);

        // Ensure Role loaded
        if (account.User.Role == null)
        {
            account.User.Role = await _context.Roles.FindAsync(account.User.RoleId)
                ?? throw new InvalidOperationException($"Role {account.User.RoleId} not found");
        }

        if (account.User.IsActive is false)
        {
            _logger.LogWarning("Google login attempt for inactive account: {Email}", normalizedEmail);
            throw AuthServiceException.Unauthorized("Account is inactive");
        }

        _logger.LogInformation("User {Email} logged in via Google successfully", normalizedEmail);
        return CreateLoginResponse(account.User, account.Username);
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Normalize và validate inputs
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);
        var trimmedName = request.Name.Trim();

        // Guard clauses
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw AuthServiceException.BadRequest("Name is required");
        }

        // Truncate if too long
        if (trimmedName.Length > NameMaxLength)
        {
            trimmedName = trimmedName[..NameMaxLength];
        }

        if (normalizedUsername.Length > UsernameMaxLength)
        {
            normalizedUsername = normalizedUsername[..UsernameMaxLength];
        }

        // Check duplicates
        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            _logger.LogWarning("Registration failed - Email already exists: {Email}", normalizedEmail);
            throw AuthServiceException.Conflict("Email already exists");
        }

        if (await _context.Accounts.AnyAsync(a => a.Username == normalizedUsername))
        {
            _logger.LogWarning("Registration failed - Username already exists: {Username}", normalizedUsername);
            throw AuthServiceException.Conflict("Username already exists");
        }

        // Create user and account in transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new DataLayer.Models.User
            {
                Email = normalizedEmail,
                FullName = trimmedName,
                RoleId = _defaultRoleId,
                IsActive = true,
                CurrentStreak = 0
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var account = new DataLayer.Models.Account
            {
                UserId = user.UserId,
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreateAt = DateTime.UtcNow
            };

            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("New user registered successfully: {Username} ({Email})", normalizedUsername, normalizedEmail);

            return new RegisterResponse
            {
                Message = "Registration successful",
                UserId = user.UserId.ToString(CultureInfo.InvariantCulture)
            };
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            var message = ResolveRegistrationConflictMessage(ex);
            _logger.LogError(ex, "Registration failed due to database conflict: {Message}", message);
            throw AuthServiceException.Conflict(message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Registration failed unexpectedly");
            throw;
        }
    }

    private async Task<Account?> FindAccountByIdentifierAsync(string identifier)
    {
        var accountsQuery = _context.Accounts
            .Where(a => a.AuthProvider == null || a.AuthProvider == string.Empty)
            .Include(a => a.User)
                .ThenInclude(u => u.Role);

        // Nếu identifier có @, tìm theo email
        if (LooksLikeEmail(identifier))
        {
            var normalizedEmail = NormalizeEmail(identifier);
            var account = await accountsQuery
                .FirstOrDefaultAsync(a => a.User.Email == normalizedEmail);

            if (account != null)
            {
                return account;
            }
        }

        // Tìm theo username
        var normalizedUsername = NormalizeUsername(identifier);
        return await accountsQuery
            .FirstOrDefaultAsync(a => a.Username == normalizedUsername);
    }

    private async Task<Account> FindOrCreateGoogleAccountAsync(
        GoogleUserInfo googleUserInfo,
        string normalizedEmail,
        string accessToken)
    {
        // Tìm account theo ProviderUserId
        var existingAccount = await _context.Accounts
            .Include(a => a.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(a =>
                a.AuthProvider == GoogleProvider &&
                a.ProviderUserId == googleUserInfo.Subject);

        if (existingAccount != null)
        {
            // Update token và username (nếu chưa có)
            existingAccount.AccessToken = accessToken;
            existingAccount.TokenExpiresAt = GetTokenExpiry(googleUserInfo);
            existingAccount.UpdateAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(existingAccount.Username))
            {
                var baseUsername = CreateUsernameCandidateFromEmail(normalizedEmail);
                existingAccount.Username = await GenerateUniqueUsernameAsync(baseUsername);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated existing Google account for user: {Email}", normalizedEmail);
            return existingAccount;
        }

        // Tạo account mới
        return await CreateGoogleAccountAsync(googleUserInfo, normalizedEmail, accessToken);
    }

    private async Task<Account> CreateGoogleAccountAsync(
        GoogleUserInfo googleUserInfo,
        string normalizedEmail,
        string accessToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Tìm hoặc tạo User
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                user = new DataLayer.Models.User
                {
                    Email = normalizedEmail,
                    FullName = googleUserInfo.Name ?? "Google User",
                    RoleId = _defaultRoleId,
                    IsActive = true,
                    CurrentStreak = 0
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new user for Google login: {Email}", normalizedEmail);
            }

            // Tạo username unique
            var baseUsername = CreateUsernameCandidateFromEmail(normalizedEmail);
            var uniqueUsername = await GenerateUniqueUsernameAsync(baseUsername);

            // Tạo Account Google
            var account = new DataLayer.Models.Account
            {
                UserId = user.UserId,
                Username = uniqueUsername,
                AuthProvider = GoogleProvider,
                ProviderUserId = googleUserInfo.Subject,
                AccessToken = accessToken,
                TokenExpiresAt = GetTokenExpiry(googleUserInfo),
                CreateAt = DateTime.UtcNow
            };

            account.User = user;
            if (user.Role != null)
            {
                account.User.Role = user.Role;
            }

            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Created new Google account for user: {Email} with username: {Username}",
                normalizedEmail, uniqueUsername);

            return account;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create Google account for email: {Email}", normalizedEmail);
            throw;
        }
    }

    private LoginResponse CreateLoginResponse(DataLayer.Models.User user, string username)
    {
        var tokenResult = _jwtTokenService.GenerateToken(user);

        return new LoginResponse
        {
            Token = tokenResult.Token,
            ExpiresIn = tokenResult.ExpiresInSeconds,
            User = new AuthUserResponse
            {
                Id = user.UserId.ToString(CultureInfo.InvariantCulture),
                Username = username,
                Email = user.Email,
                Name = user.FullName
            }
        };
    }


    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
    {
        var normalizedBase = NormalizeUsername(baseUsername);

        // Truncate để có chỗ cho suffix
        if (normalizedBase.Length > UsernameMaxLength - 3)
        {
            normalizedBase = normalizedBase[..(UsernameMaxLength - 3)];
        }

        var candidate = normalizedBase;
        var suffix = 0;

        while (await _context.Accounts.AnyAsync(a => a.Username == candidate))
        {
            suffix++;
            var suffixStr = suffix.ToString(CultureInfo.InvariantCulture);
            var maxBaseLength = UsernameMaxLength - suffixStr.Length - 1; // -1 cho dấu '-'

            if (normalizedBase.Length > maxBaseLength)
            {
                candidate = $"{normalizedBase[..maxBaseLength]}-{suffixStr}";
            }
            else
            {
                candidate = $"{normalizedBase}-{suffixStr}";
            }
        }

        return candidate;
    }

    private static string CreateUsernameCandidateFromEmail(string normalizedEmail)
    {
        var atIndex = normalizedEmail.IndexOf('@');
        var localPart = atIndex > 0 ? normalizedEmail[..atIndex] : normalizedEmail;

        var sb = new StringBuilder(localPart.Length);
        foreach (var c in localPart)
        {
            if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-')
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "user" : result;
    }


    private static string ResolveRegistrationConflictMessage(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        if (message.Contains("IX_Users_Email", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Users.Email", StringComparison.OrdinalIgnoreCase))
        {
            return "Email already exists";
        }

        if (message.Contains("IX_Accounts_Username", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Accounts.Username", StringComparison.OrdinalIgnoreCase))
        {
            return "Username already exists";
        }

        return "Registration failed due to duplicate data";
    }


    private static DateTime? GetTokenExpiry(GoogleUserInfo googleUserInfo)
    {
        if (googleUserInfo.ExpirationTimeSeconds.HasValue)
        {
            return DateTimeOffset.FromUnixTimeSeconds(googleUserInfo.ExpirationTimeSeconds.Value).UtcDateTime;
        }

        return null;
    }


    private static bool LooksLikeEmail(string value) => value.Contains('@');


    private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();


    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

}


public sealed class AuthServiceException : Exception
{
    public AuthServiceException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public static AuthServiceException BadRequest(string message) =>
        new(message, StatusCodes.Status400BadRequest);

    public static AuthServiceException Unauthorized(string message) =>
        new(message, StatusCodes.Status401Unauthorized);

    public static AuthServiceException NotFound(string message) =>
        new(message, StatusCodes.Status404NotFound);

    public static AuthServiceException Conflict(string message) =>
        new(message, StatusCodes.Status409Conflict);

    public static AuthServiceException ServerError(string message) =>
        new(message, StatusCodes.Status500InternalServerError);
}

