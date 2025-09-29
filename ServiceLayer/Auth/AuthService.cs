using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceLayer.Email;

namespace ServiceLayer.Auth;

public class AuthService : IAuthService
{
    private const string GoogleProvider = "Google";
    private const int UsernameMaxLength = 20;

    private readonly LuminaSystemContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly string? _googleClientId;
    private readonly int _defaultRoleId;

    public AuthService(
        LuminaSystemContext context,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _googleClientId = configuration["Google:ClientId"];
        _defaultRoleId = 4;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequestDTO request, CancellationToken cancellationToken)
    {
        var identifier = request.Username.Trim();
        var accountsQuery = _context.Accounts
            .Include(a => a.User).ThenInclude(u => u.Role)
            .Where(a => string.IsNullOrEmpty(a.AuthProvider));

        Account? account = null;
        if (LooksLikeEmail(identifier))
        {
            var email = NormalizeEmail(identifier);
            account = await accountsQuery.FirstOrDefaultAsync(a => a.User.Email == email, cancellationToken);
        }

        if (account == null)
        {
            var username = NormalizeUsername(identifier);
            account = await accountsQuery.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
        }

        if (account?.User == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            throw AuthServiceException.Unauthorized("Invalid username or password");
        }

        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);
        if (!passwordMatches)
        {
            throw AuthServiceException.Unauthorized("Invalid username or password");
        }

        if (account.User.IsActive is false)
        {
            throw AuthServiceException.Unauthorized("Account is inactive");
        }

        var token = _jwtTokenService.GenerateToken(account.User);
        var response = new LoginResponse
        {
            Token = token.Token,
            ExpiresIn = token.ExpiresInSeconds,
            User = new AuthUserResponse
            {
                Id = account.User.UserId.ToString(CultureInfo.InvariantCulture),
                Username = account.Username,
                Email = account.User.Email,
                Name = account.User.FullName
            }
        };

        return response;
    }

    public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_googleClientId))
        {
            _logger.LogError("Google login was attempted but the ClientId is not configured.");
            throw AuthServiceException.ServerError("Google login is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.Token,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _googleClientId } });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google token received.");
            throw AuthServiceException.Unauthorized("Invalid Google token.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Google token.");
            throw AuthServiceException.ServerError("Failed to verify Google token.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw AuthServiceException.BadRequest("Google account email is required.");
        }

        var normalizedEmail = NormalizeEmail(payload.Email);
        var account = await _context.Accounts
            .Include(a => a.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(
                a => a.AuthProvider == GoogleProvider && a.ProviderUserId == payload.Subject,
                cancellationToken);

        if (account == null)
        {
            try
            {
                account = await UpsertGoogleAccountAsync(payload, normalizedEmail, request.Token, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Unable to upsert Google account for {Email}", normalizedEmail);
                throw AuthServiceException.ServerError("Unable to complete Google login.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google login for {Email}", normalizedEmail);
                throw AuthServiceException.ServerError("Unable to complete Google login.");
            }
        }

        if (account.User.IsActive is false)
        {
            throw AuthServiceException.Unauthorized("Account is inactive");
        }

        var token = _jwtTokenService.GenerateToken(account.User);
        var response = new LoginResponse
        {
            Token = token.Token,
            ExpiresIn = token.ExpiresInSeconds,
            User = new AuthUserResponse
            {
                Id = account.User.UserId.ToString(CultureInfo.InvariantCulture),
                Username = account.Username,
                Email = account.User.Email,
                Name = account.User.FullName
            }
        };

        return response;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var trimmedName = request.Name.Trim();
        var normalizedUsername = NormalizeUsername(request.Username);

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw AuthServiceException.BadRequest("Name is required.");
        }

        if (trimmedName.Length > 50)
        {
            trimmedName = trimmedName[..50];
        }

        if (string.IsNullOrEmpty(normalizedUsername))
        {
            throw AuthServiceException.BadRequest("Username is required.");
        }

        if (normalizedUsername.Length > UsernameMaxLength)
        {
            normalizedUsername = normalizedUsername[..UsernameMaxLength];
        }

        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw AuthServiceException.Conflict("Email already exists");
        }

        if (await _context.Accounts.AnyAsync(a => a.Username == normalizedUsername, cancellationToken))
        {
            throw AuthServiceException.Conflict("Username already exists");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new User
            {
                Email = normalizedEmail,
                FullName = trimmedName,
                RoleId = _defaultRoleId,
                IsActive = true
            };

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var account = new Account
            {
                UserId = user.UserId,
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _context.Accounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var response = new RegisterResponse
            {
                Message = "User registered successfully",
                UserId = user.UserId.ToString(CultureInfo.InvariantCulture)
            };

            return response;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to register user for email {Email}", normalizedEmail);
            var message = ResolveRegistrationConflictMessage(ex);
            throw AuthServiceException.Conflict(message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error registering user for email {Email}", normalizedEmail);
            throw AuthServiceException.ServerError("Failed to register user.");
        }
    }

    private async Task<Account> UpsertGoogleAccountAsync(
        GoogleJsonWebSignature.Payload payload,
        string normalizedEmail,
        string accessToken,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
            if (user == null)
            {
                var googleName = (payload.Name ?? payload.Email ?? "Google User").Trim();
                if (string.IsNullOrWhiteSpace(googleName))
                {
                    googleName = "Google User";
                }

                if (googleName.Length > 50)
                {
                    googleName = googleName[..50];
                }

                user = new User
                {
                    Email = normalizedEmail,
                    FullName = googleName,
                    RoleId = _defaultRoleId,
                    IsActive = true
                };
                await _context.Users.AddAsync(user, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.AuthProvider == GoogleProvider, cancellationToken);

            if (account == null)
            {
                account = new Account
                {
                    UserId = user.UserId,
                    AuthProvider = GoogleProvider,
                    ProviderUserId = payload.Subject,
                    AccessToken = accessToken,
                    TokenExpiresAt = GetTokenExpiry(payload)
                };

                account.Username = await GenerateUniqueUsernameAsync(CreateUsernameCandidateFromEmail(normalizedEmail), cancellationToken);
                await _context.Accounts.AddAsync(account, cancellationToken);
            }
            else
            {
                account.AccessToken = accessToken;
                account.TokenExpiresAt = GetTokenExpiry(payload);
                account.UpdateAt = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(account.Username))
                {
                    var usernameSeed = CreateUsernameCandidateFromEmail(normalizedEmail);
                    account.Username = await GenerateUniqueUsernameAsync(usernameSeed, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await _context.Accounts
                .Include(a => a.User)
                .FirstAsync(a => a.AccountId == account.AccountId, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to upsert Google account for email {Email}", normalizedEmail);
            throw new InvalidOperationException("Unable to link Google account at this time.", ex);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static DateTime? GetTokenExpiry(GoogleJsonWebSignature.Payload payload)
        => payload.ExpirationTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(payload.ExpirationTimeSeconds.Value).UtcDateTime
            : null;

    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var normalizedBase = NormalizeUsername(baseUsername);
        if (string.IsNullOrEmpty(normalizedBase))
        {
            normalizedBase = "user";
        }

        if (normalizedBase.Length > UsernameMaxLength)
        {
            normalizedBase = normalizedBase[..UsernameMaxLength];
        }

        var candidate = normalizedBase;
        var suffix = 0;
        while (await _context.Accounts.AnyAsync(a => a.Username == candidate, cancellationToken))
        {
            suffix++;
            var suffixText = $"-{suffix}";
            var maxBaseLength = UsernameMaxLength - suffixText.Length;
            if (maxBaseLength < 1)
            {
                maxBaseLength = 1;
            }

            var truncatedBase = normalizedBase.Length > maxBaseLength
                ? normalizedBase[..maxBaseLength]
                : normalizedBase;

            candidate = $"{truncatedBase}{suffixText}";
        }

        return candidate;
    }

    private static string ResolveRegistrationConflictMessage(DbUpdateException exception)
    {
        var innerMessage = exception.InnerException?.Message;
        if (!string.IsNullOrWhiteSpace(innerMessage))
        {
            if (innerMessage.Contains("Username", StringComparison.OrdinalIgnoreCase))
            {
                return "Username already exists";
            }

            if (innerMessage.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                return "Email already exists";
            }
        }

        return "Email or username already exists";
    }

    private static string CreateUsernameCandidateFromEmail(string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return "user";
        }

        var localPart = normalizedEmail.Split('@')[0];
        if (string.IsNullOrWhiteSpace(localPart))
        {
            localPart = normalizedEmail;
        }

        var builder = new StringBuilder(localPart.Length);
        foreach (var character in localPart)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        if (builder.Length == 0)
        {
            builder.Append("user");
        }

        var candidate = builder.ToString();
        return candidate.Length > UsernameMaxLength ? candidate[..UsernameMaxLength] : candidate;
    }

    private static bool LooksLikeEmail(string value) => value.Contains('@');
    private static string NormalizeUsername(string username)
        => string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim().ToLowerInvariant();
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
public class AuthServiceException : Exception
{
    public AuthServiceException(string message, int statusCode)
        : base(message)
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