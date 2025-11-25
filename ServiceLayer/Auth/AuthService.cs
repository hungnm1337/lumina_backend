using System.Globalization;
using System.Text;
using System.Text.Json;
using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceLayer.Email;

namespace ServiceLayer.Auth;


public sealed class AuthService : IAuthService
{

    private const string GoogleProvider = "Google";
    private const int UsernameMaxLength = 20;
    private const int NameMaxLength = 50;



    private readonly LuminaSystemContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;
    private readonly int _defaultRoleId = 4;
    private readonly int _registrationOtpLength = 6;
    private readonly int _registrationOtpExpiryMinutes = 10;
    private readonly int _refreshTokenExpirationDays = 7;



    public AuthService(
        LuminaSystemContext context,
        IJwtTokenService jwtTokenService,
        IGoogleAuthService googleAuthService,
        IEmailSender emailSender,
        ILogger<AuthService> logger
        )
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _googleAuthService = googleAuthService;
        _emailSender = emailSender;
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
        return await CreateLoginResponseAsync(account.User, account.Username);
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
        return await CreateLoginResponseAsync(account.User, account.Username);
    }

    public async Task<SendRegistrationOtpResponse> SendRegistrationOtpAsync(SendRegistrationOtpRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);

        // Check cả email và username trong 1 query để tối ưu performance
        var conflicts = await _context.Accounts
            .Include(a => a.User)
            .Where(a => a.Username == normalizedUsername || (a.User.Email == normalizedEmail && a.User.IsActive == true))
            .Select(a => new { a.Username, a.User.Email, a.User.IsActive })
            .ToListAsync();

        // Kiểm tra email đã được đăng ký
        var emailExists = conflicts.Any(c => c.Email == normalizedEmail && c.IsActive == true);
        if (emailExists)
        {
            _logger.LogWarning("Registration OTP request failed - Email already registered: {Email}", normalizedEmail);
            throw AuthServiceException.Conflict("Email đã được đăng ký");
        }

        // Kiểm tra username đã tồn tại
        var usernameExists = conflicts.Any(c => c.Username == normalizedUsername);
        if (usernameExists)
        {
            _logger.LogWarning("Registration OTP request failed - Username already exists: {Username}", normalizedUsername);
            throw AuthServiceException.Conflict("Tên đăng nhập đã tồn tại");
        }

        // Cleanup: Xóa temp user cũ (IsActive=false, không có Account) nếu tồn tại
        var tempUserOld = await _context.Users
            .Include(u => u.PasswordResetTokens)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive == false);
            
        if (tempUserOld != null)
        {
            // Xóa các token cũ của temp user
            if (tempUserOld.PasswordResetTokens != null && tempUserOld.PasswordResetTokens.Count > 0)
            {
                _context.PasswordResetTokens.RemoveRange(tempUserOld.PasswordResetTokens);
            }
            
            // Xóa temp user cũ
            _context.Users.Remove(tempUserOld);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up old temp user for {Email}", normalizedEmail);
        }

        // Tạo OTP code
        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

        var now = DateTime.UtcNow;
        
        // Tạo temp user mới với IsActive = false (chưa verify OTP)
        var tempUser = new DataLayer.Models.User
        {
            Email = normalizedEmail,
            FullName = "Pending", // Placeholder - sẽ được cập nhật khi verify
            RoleId = _defaultRoleId,
            IsActive = false,
            CurrentStreak = 0
        };
        
        await _context.Users.AddAsync(tempUser);
        await _context.SaveChangesAsync();

        var registrationToken = new PasswordResetToken
        {
            UserId = tempUser.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_registrationOtpExpiryMinutes)
        };

        await _context.PasswordResetTokens.AddAsync(registrationToken);
        await _context.SaveChangesAsync();

        // Gửi email OTP
        try
        {
            await _emailSender.SendRegistrationOtpAsync(normalizedEmail, normalizedEmail, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration OTP to {Email}", normalizedEmail);
            
            // Rollback: Xóa token và temp user
            _context.PasswordResetTokens.Remove(registrationToken);
            _context.Users.Remove(tempUser);
            await _context.SaveChangesAsync();
            
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
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);
        var trimmedName = request.Name.Trim();

        // Validate inputs
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw AuthServiceException.BadRequest("Tên không được để trống");
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

        // Tìm temp user (Include Role để tạo JWT token)
        var tempUser = await _context.Users
            .Include(u => u.Accounts)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive == false);

        if (tempUser == null)
        {
            throw AuthServiceException.NotFound("Không tìm thấy thông tin đăng ký. Vui lòng yêu cầu mã OTP mới.");
        }

        // Kiểm tra OTP token
        var registrationToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => 
                t.UserId == tempUser.UserId && 
                t.UsedAt == null && 
                t.ExpiresAt > DateTime.UtcNow);

        if (registrationToken == null)
        {
            throw AuthServiceException.BadRequest("OTP không hợp lệ hoặc đã hết hạn");
        }

        // Verify OTP
        if (!BCrypt.Net.BCrypt.Verify(request.OtpCode, registrationToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Mã OTP không đúng");
        }

        // Kiểm tra email đã được đăng ký chưa (double check)
        var existingActiveUser = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail && u.IsActive == true);
            
        if (existingActiveUser)
        {
            throw AuthServiceException.Conflict("Email đã được đăng ký");
        }

        // Kiểm tra username đã tồn tại chưa
        var usernameExists = await _context.Accounts
            .AnyAsync(a => a.Username == normalizedUsername);
            
        if (usernameExists)
        {
            throw AuthServiceException.Conflict("Tên đăng nhập đã tồn tại");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Update thông tin user thật
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

            await _context.Accounts.AddAsync(newAccount);

            // Đánh dấu token đã sử dụng
            registrationToken.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("User registration completed successfully: {Username} ({Email})", 
                normalizedUsername, normalizedEmail);

            // Tạo JWT token và refresh token để user có thể login ngay
            var tokenResult = _jwtTokenService.GenerateToken(tempUser);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            
            // Create refresh token entity
            var refreshTokenEntity = new RefreshToken
            {
                UserId = tempUser.UserId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
            };
            
            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

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
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to complete registration for {Email}", normalizedEmail);
            throw;
        }
    }

    public async Task<ResendOtpResponse> ResendRegistrationOtpAsync(ResendRegistrationOtpRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        // Tìm user chưa active
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive == false);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Không tìm thấy thông tin đăng ký");
        }

        // Xóa token cũ
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.UserId && t.UsedAt == null)
            .ToListAsync();

        if (existingTokens.Count > 0)
        {
            _context.PasswordResetTokens.RemoveRange(existingTokens);
        }

        // Tạo OTP mới
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

        await _context.PasswordResetTokens.AddAsync(newToken);
        await _context.SaveChangesAsync();

        // Gửi email
        try
        {
            await _emailSender.SendRegistrationOtpAsync(user.Email, user.FullName, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend registration OTP to {Email}", user.Email);
            
            // Xóa token vừa tạo
            _context.PasswordResetTokens.Remove(newToken);
            await _context.SaveChangesAsync();
            
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

    // Helper method to create LoginResponse with refresh token
    private async Task<LoginResponse> CreateLoginResponseAsync(DataLayer.Models.User user, string username)
    {
        var tokenResult = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        
        // Revoke old refresh tokens for this user
        var now = DateTime.UtcNow;
        var existingTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.UserId 
                      && !rt.IsRevoked 
                      && rt.ExpiresAt > now)
            .ToListAsync();
        
        foreach (var token in existingTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "Replaced by new token";
        }
        
        // Create new refresh token
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays)
        };
        
        await _context.RefreshTokens.AddAsync(refreshTokenEntity);
        await _context.SaveChangesAsync();
        
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
        var refreshTokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
            .Include(rt => rt.User.Accounts)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
        
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
        
        // Revoke the used refresh token
        refreshTokenEntity.IsRevoked = true;
        refreshTokenEntity.RevokedAt = DateTime.UtcNow;
        refreshTokenEntity.RevokedReason = "Token refreshed";
        
        // Get username from account
        var account = refreshTokenEntity.User.Accounts.FirstOrDefault();
        var username = account?.Username ?? refreshTokenEntity.User.Email;
        
        _logger.LogInformation("Refresh token used successfully for user: {UserId}", refreshTokenEntity.UserId);
        
        // Generate new tokens
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

