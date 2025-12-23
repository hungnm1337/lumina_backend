using System.Globalization;
using System.Text;
using System.Text.Json;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepositoryLayer.UnitOfWork;
using ServiceLayer.Email;

namespace ServiceLayer.Auth;


public sealed class AuthService : IAuthService
{

    private const string GoogleProvider = "Google";
    private const int UsernameMaxLength = 20;
    private const int NameMaxLength = 50;



    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;
    private readonly int _defaultRoleId = 4;
    private readonly int _registrationOtpLength = 6;
    private readonly int _registrationOtpExpiryMinutes = 10;
    private readonly int _refreshTokenExpirationDays = 7;



    public AuthService(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IGoogleAuthService googleAuthService,
        IEmailSender emailSender,
        ILogger<AuthService> logger
        )
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _googleAuthService = googleAuthService;
        _emailSender = emailSender;
        _logger = logger;
    }


    public async Task<LoginResponse> LoginAsync(LoginRequestDTO request)
    {
        var identifier = request.Username.Trim();
        var account = await _unitOfWork.Accounts.FindByIdentifierAsync(identifier);

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
        return await CreateLoginResponseAsync(account.User, account.Username);
    }

    public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
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

        if (account.User.Role == null)
        {
            account.User.Role = await _unitOfWork.Roles.GetAsync(r => r.RoleId == account.User.RoleId)
                ?? throw new InvalidOperationException($"Role {account.User.RoleId} not found");
        }

        if (account.User.IsActive is false)
        {
            _logger.LogWarning("Google login attempt for inactive account: {Email}", normalizedEmail);
            throw AuthServiceException.Unauthorized("Account is inactive");
        }

        _logger.LogInformation("User {Email} logged in via Google successfully", normalizedEmail);
        return await CreateLoginResponseAsync(account.User, account.Username);
    }

    public async Task<SendRegistrationOtpResponse> SendRegistrationOtpAsync(SendRegistrationOtpRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);

        var emailExists = await _unitOfWork.Users.EmailExistsAsync(normalizedEmail, activeOnly: true);
        if (emailExists)
        {
            _logger.LogWarning("Registration OTP request failed - Email already registered: {Email}", normalizedEmail);
            throw AuthServiceException.Conflict("Email đã được đăng ký");
        }

        var usernameExists = await _unitOfWork.Accounts.UsernameExistsAsync(normalizedUsername);
        if (usernameExists)
        {
            _logger.LogWarning("Registration OTP request failed - Username already exists: {Username}", normalizedUsername);
            throw AuthServiceException.Conflict("Tên đăng nhập đã tồn tại");
        }

        var tempUserOld = await _unitOfWork.Users.GetInactiveByEmailWithTokensAsync(normalizedEmail);

        if (tempUserOld != null)
        {
            if (tempUserOld.PasswordResetTokens != null && tempUserOld.PasswordResetTokens.Count > 0)
            {
                _unitOfWork.PasswordResetTokens.RemoveRange(tempUserOld.PasswordResetTokens);
            }

            _unitOfWork.Users.Remove(tempUserOld);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Cleaned up old temp user for {Email}", normalizedEmail);
        }

        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

        var now = DateTime.UtcNow;

        var tempUser = new DataLayer.Models.User
        {
            Email = normalizedEmail,
            FullName = "Pending",
            RoleId = _defaultRoleId,
            IsActive = false,
            CurrentStreak = 0
        };

        await _unitOfWork.Users.AddAsync(tempUser);
        await _unitOfWork.CompleteAsync();

        var registrationToken = new PasswordResetToken
        {
            UserId = tempUser.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_registrationOtpExpiryMinutes)
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(registrationToken);
        await _unitOfWork.CompleteAsync();

        try
        {
            await _emailSender.SendRegistrationOtpAsync(normalizedEmail, normalizedEmail, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration OTP to {Email}", normalizedEmail);

            _unitOfWork.PasswordResetTokens.Remove(registrationToken);
            _unitOfWork.Users.Remove(tempUser);
            await _unitOfWork.CompleteAsync();

            throw AuthServiceException.ServerError("Không thể gửi mã OTP. Vui lòng thử lại sau.");
        }

        _logger.LogInformation("Registration OTP sent to {Email}", normalizedEmail);

        return new SendRegistrationOtpResponse
        {
            Message = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra email để hoàn tất đăng ký."
        };
    }

    public async Task<VerifyRegistrationResponse> VerifyRegistrationAsync(VerifyRegistrationRequest request)
    {
        _logger.LogInformation("=== VerifyRegistrationAsync START ===");
        _logger.LogInformation("Request Email: {Email}, Username: {Username}, OtpCode: {OtpCode}", 
            request.Email, request.Username, request.OtpCode);

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);
        var trimmedName = request.Name.Trim();

        _logger.LogInformation("Normalized Email: {Email}, Username: {Username}", normalizedEmail, normalizedUsername);

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            _logger.LogWarning("Name is empty");
            throw AuthServiceException.BadRequest("Tên không được để trống");
        }

        if (trimmedName.Length > NameMaxLength)
        {
            trimmedName = trimmedName[..NameMaxLength];
        }

        if (normalizedUsername.Length > UsernameMaxLength)
        {
            normalizedUsername = normalizedUsername[..UsernameMaxLength];
        }

        // First check if email is already registered with an active user
        var activeUserExists = await _unitOfWork.Users.EmailExistsAsync(normalizedEmail, activeOnly: true);
        _logger.LogInformation("Active user exists check: {Exists}", activeUserExists);
        
        if (activeUserExists)
        {
            _logger.LogWarning("Email already registered as active: {Email}", normalizedEmail);
            throw AuthServiceException.Conflict("Email đã được đăng ký");
        }

        var tempUser = await _unitOfWork.Users.GetInactiveByEmailWithTokensAsync(normalizedEmail);
        _logger.LogInformation("TempUser found: {Found}, UserId: {UserId}", 
            tempUser != null, tempUser?.UserId);

        if (tempUser == null)
        {
            // Check if this email already has an active account (registration was completed)
            var existingActiveUser = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
            _logger.LogInformation("Existing active user check: {Found}, IsActive: {IsActive}",
                existingActiveUser != null, existingActiveUser?.IsActive);
            
            if (existingActiveUser != null && existingActiveUser.IsActive == true)
            {
                _logger.LogWarning("Registration already completed for: {Email}", normalizedEmail);
                // Registration was already completed - this is likely a retry after network error
                throw AuthServiceException.Conflict("Tài khoản đã được đăng ký thành công. Vui lòng đăng nhập.");
            }
            
            _logger.LogWarning("No temp user found for: {Email}", normalizedEmail);
            throw AuthServiceException.NotFound("Không tìm thấy thông tin đăng ký. Vui lòng yêu cầu mã OTP mới.");
        }

        var registrationToken = await _unitOfWork.PasswordResetTokens.GetActiveTokenByUserIdAsync(tempUser.UserId, asTracking: true);
        _logger.LogInformation("Registration token found: {Found}, ExpiresAt: {ExpiresAt}", 
            registrationToken != null, registrationToken?.ExpiresAt);

        if (registrationToken == null)
        {
            // Check if there's a used token - meaning registration might have completed
            var usedToken = await _unitOfWork.PasswordResetTokens.GetUsedTokenByUserIdAsync(tempUser.UserId);
            _logger.LogInformation("Used token check: {Found}", usedToken != null);
            
            if (usedToken != null)
            {
                _logger.LogWarning("OTP already used for user: {UserId}", tempUser.UserId);
                throw AuthServiceException.Conflict("Mã OTP đã được sử dụng. Vui lòng thử đăng nhập.");
            }
            
            _logger.LogWarning("No active token found, OTP expired for user: {UserId}", tempUser.UserId);
            throw AuthServiceException.BadRequest("OTP không hợp lệ hoặc đã hết hạn");
        }

        var otpVerified = BCrypt.Net.BCrypt.Verify(request.OtpCode, registrationToken.CodeHash);
        _logger.LogInformation("OTP verification result: {Result}", otpVerified);

        if (!otpVerified)
        {
            _logger.LogWarning("OTP mismatch for user: {UserId}", tempUser.UserId);
            throw AuthServiceException.BadRequest("Mã OTP không đúng");
        }

        var usernameExists = await _unitOfWork.Accounts.UsernameExistsAsync(normalizedUsername);

        if (usernameExists)
        {
            throw AuthServiceException.Conflict("Tên đăng nhập đã tồn tại");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            tempUser.FullName = trimmedName;
            tempUser.IsActive = true;

            // Tạo Account
            var newAccount = new Account
            {
                UserId = tempUser.UserId,
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreateAt = DateTime.UtcNow
            };

            await _unitOfWork.Accounts.AddAsync(newAccount);

            registrationToken.UsedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User registration completed successfully: {Username} ({Email})",
                normalizedUsername, normalizedEmail);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to complete registration for {Email}", normalizedEmail);
            throw;
        }

        // Generate tokens AFTER transaction commit succeeds
        // If token generation fails, user is still registered successfully
        try
        {
            // Load Role for JWT token generation (needed for role claims)
            if (tempUser.Role == null)
            {
                tempUser.Role = await _unitOfWork.Roles.GetAsync(r => r.RoleId == tempUser.RoleId)
                    ?? throw new InvalidOperationException($"Role {tempUser.RoleId} not found");
                _logger.LogInformation("Loaded role for user: {RoleId}", tempUser.RoleId);
            }

            var tokenResult = _jwtTokenService.GenerateToken(tempUser);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = tempUser.UserId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
            await _unitOfWork.CompleteAsync();

            return new VerifyRegistrationResponse
            {
                Message = "Đăng ký thành công!",
                Token = tokenResult.Token,
                RefreshToken = refreshToken,
                ExpiresIn = tokenResult.ExpiresInSeconds,
                RefreshExpiresIn = (int)TimeSpan.FromDays(_refreshTokenExpirationDays).TotalSeconds,
                User = new AuthUserResponse
                {
                    Id = tempUser.UserId.ToString(CultureInfo.InvariantCulture),
                    Username = normalizedUsername,
                    Email = normalizedEmail,
                    Name = trimmedName
                }
            };
        }
        catch (Exception ex)
        {
            // Registration completed but token generation failed
            // User can still login with their credentials
            _logger.LogError(ex, "Registration succeeded but token generation failed for {Email}. User can login normally.", normalizedEmail);
            throw AuthServiceException.ServerError("Đăng ký thành công nhưng không thể tạo token. Vui lòng đăng nhập lại.");
        }
    }

    public async Task<ResendOtpResponse> ResendRegistrationOtpAsync(ResendRegistrationOtpRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);

        if (user == null || user.IsActive == true)
        {
            throw AuthServiceException.NotFound("Không tìm thấy thông tin đăng ký");
        }

        var existingTokens = await _unitOfWork.PasswordResetTokens.GetUnusedTokensByUserIdAsync(user.UserId);

        if (existingTokens.Count > 0)
        {
            _unitOfWork.PasswordResetTokens.RemoveRange(existingTokens);
        }

        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_registrationOtpExpiryMinutes);

        var newToken = new PasswordResetToken
        {
            UserId = user.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(newToken);
        await _unitOfWork.CompleteAsync();

        try
        {
            await _emailSender.SendRegistrationOtpAsync(user.Email, user.FullName, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend registration OTP to {Email}", user.Email);

            _unitOfWork.PasswordResetTokens.Remove(newToken);
            await _unitOfWork.CompleteAsync();

            throw AuthServiceException.ServerError("Không thể gửi mã OTP. Vui lòng thử lại sau.");
        }

        _logger.LogInformation("Registration OTP resent to {Email}", user.Email);

        return new ResendOtpResponse
        {
            Message = "Mã OTP mới đã được gửi đến email của bạn"
        };
    }

    private string GenerateOtpCode()
    {
        var random = new Random();
        var code = random.Next(0, 999999).ToString(CultureInfo.InvariantCulture);
        return code.PadLeft(_registrationOtpLength, '0');
    }

    private async Task<Account> FindOrCreateGoogleAccountAsync(
        GoogleUserInfo googleUserInfo,
        string normalizedEmail,
        string accessToken)
    {
        var existingAccount = await _unitOfWork.Accounts.GetByGoogleIdWithUserAndRoleAsync(googleUserInfo.Subject);

        if (existingAccount != null)
        {
            existingAccount.AccessToken = accessToken;
            existingAccount.TokenExpiresAt = GetTokenExpiry(googleUserInfo);
            existingAccount.UpdateAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(existingAccount.Username))
            {
                var baseUsername = CreateUsernameCandidateFromEmail(normalizedEmail);
                existingAccount.Username = await _unitOfWork.Accounts.GenerateUniqueUsernameAsync(baseUsername, UsernameMaxLength);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Updated existing Google account for user: {Email}", normalizedEmail);
            return existingAccount;
        }

        return await CreateGoogleAccountAsync(googleUserInfo, normalizedEmail, accessToken);
    }

    private async Task<Account> CreateGoogleAccountAsync(
        GoogleUserInfo googleUserInfo,
        string normalizedEmail,
        string accessToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var user = await _unitOfWork.Users.GetByEmailWithAccountsAndRoleAsync(normalizedEmail);

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

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Created new user for Google login: {Email}", normalizedEmail);
            }

            var baseUsername = CreateUsernameCandidateFromEmail(normalizedEmail);
            var uniqueUsername = await _unitOfWork.Accounts.GenerateUniqueUsernameAsync(baseUsername, UsernameMaxLength);

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

            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.CompleteAsync();

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

    private async Task<LoginResponse> CreateLoginResponseAsync(DataLayer.Models.User user, string username)
    {
        var tokenResult = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Revoke all existing active refresh tokens
        await _unitOfWork.RefreshTokens.RevokeAllActiveByUserIdAsync(user.UserId, "Replaced by new token");

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.CompleteAsync();

        return new LoginResponse
        {
            Token = tokenResult.Token,
            RefreshToken = refreshToken,
            ExpiresIn = tokenResult.ExpiresInSeconds,
            RefreshExpiresIn = (int)TimeSpan.FromDays(_refreshTokenExpirationDays).TotalSeconds,
            User = new AuthUserResponse
            {
                Id = user.UserId.ToString(CultureInfo.InvariantCulture),
                Username = username,
                Email = user.Email,
                Name = user.FullName
            }
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var now = DateTime.UtcNow;
        var refreshTokenEntity = await _unitOfWork.RefreshTokens.GetByTokenWithUserAndAccountsAsync(request.RefreshToken);

        if (refreshTokenEntity == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw AuthServiceException.Unauthorized("Invalid refresh token");
        }

        if (refreshTokenEntity.IsRevoked)
        {
            _logger.LogWarning("Refresh token is revoked. UserId: {UserId}", refreshTokenEntity.UserId);
            throw AuthServiceException.Unauthorized("Refresh token has been revoked");
        }

        if (refreshTokenEntity.ExpiresAt <= now)
        {
            _logger.LogWarning("Refresh token is expired. UserId: {UserId}, ExpiresAt: {ExpiresAt}",
                refreshTokenEntity.UserId, refreshTokenEntity.ExpiresAt);
            throw AuthServiceException.Unauthorized("Refresh token has expired");
        }

        if (refreshTokenEntity.User.IsActive is false)
        {
            _logger.LogWarning("Refresh token attempt for inactive user: {UserId}", refreshTokenEntity.UserId);
            throw AuthServiceException.Unauthorized("User account is inactive");
        }

        refreshTokenEntity.IsRevoked = true;
        refreshTokenEntity.RevokedAt = DateTime.UtcNow;
        refreshTokenEntity.RevokedReason = "Token refreshed";
        _unitOfWork.RefreshTokens.Update(refreshTokenEntity);

        var account = refreshTokenEntity.User.Accounts.FirstOrDefault();
        var username = account?.Username ?? refreshTokenEntity.User.Email;

        _logger.LogInformation("Refresh token used successfully for user: {UserId}", refreshTokenEntity.UserId);

        return await CreateLoginResponseAsync(refreshTokenEntity.User, username);
    }

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

