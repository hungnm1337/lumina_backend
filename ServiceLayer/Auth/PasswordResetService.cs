using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceLayer.Email;
using System.Security.Cryptography;

namespace ServiceLayer.Auth;


public interface IPasswordResetService
{
    
    Task<ForgotPasswordResponse> SendPasswordResetCodeAsync(ForgotPasswordRequest request);

    
    Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request);

    
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
}


public sealed class PasswordResetService : IPasswordResetService
{
    private readonly LuminaSystemContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly int _passwordResetCodeLength;
    private readonly int _passwordResetExpiryMinutes;

    public PasswordResetService(
        LuminaSystemContext context,
        IEmailSender emailSender,
        ILogger<PasswordResetService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
        // Đảm bảo giá trị cấu hình trong khoảng hợp lý
        _passwordResetCodeLength = Math.Clamp(configuration.GetValue("PasswordReset:CodeLength", 6), 4, 12);
        _passwordResetExpiryMinutes = Math.Clamp(configuration.GetValue("PasswordReset:CodeExpiryMinutes", 10), 1, 60);
    }

    
    public async Task<ForgotPasswordResponse> SendPasswordResetCodeAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        // Tìm user theo email
        var user = await _context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Guard clause: User không tồn tại
        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        // Guard clause: Kiểm tra account có password
        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("This account does not have a password set");
        }

        // Tạo OTP code ngẫu nhiên
        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_passwordResetExpiryMinutes);

        // Vô hiệu hóa các token cũ chưa sử dụng (chống replay attack)
        var existingTokens = await _context.PasswordResetTokens
            .Where(token => token.UserId == user.UserId && token.UsedAt == null)
            .ToListAsync();

        if (existingTokens.Count > 0)
        {
            _context.PasswordResetTokens.RemoveRange(existingTokens);
        }

        // Tạo token mới
        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _context.PasswordResetTokens.AddAsync(resetToken);
        await _context.SaveChangesAsync();

        // Gửi email OTP
        try
        {
            await _emailSender.SendPasswordResetCodeAsync(user.Email, user.FullName, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email);
            // Vô hiệu hóa token nếu gửi email thất bại
            resetToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw AuthServiceException.ServerError("Failed to send OTP email");
        }

        _logger.LogInformation("Password reset OTP generated for user {UserId}", user.UserId);

        return new ForgotPasswordResponse { Message = "An OTP has been sent to your email" };
    }

   
    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        // Tìm user
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Guard clause: User không tồn tại
        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        // Lấy token active
        var resetToken = await GetActiveResetTokenAsync(user.UserId, asTracking: false);

        // Guard clause: Token không hợp lệ hoặc OTP sai
        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code");
        }

        return new VerifyResetCodeResponse { Message = "OTP verified successfully" };
    }

    
    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        // Tìm user với account
        var user = await _context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        // Guard clause: User không tồn tại
        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        // Guard clause: Kiểm tra account có password
        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null)
        {
            throw AuthServiceException.BadRequest("This account does not support password login");
        }

        // Lấy token active (tracking để có thể update)
        var resetToken = await GetActiveResetTokenAsync(user.UserId, asTracking: true);

        // Guard clause: Token không hợp lệ hoặc OTP sai
        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code");
        }

        // Guard clause: Mật khẩu mới không được trùng mật khẩu cũ
        if (!string.IsNullOrEmpty(account.PasswordHash) && BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("New password must be different from the current password");
        }

        // Cập nhật mật khẩu và vô hiệu hóa token
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.UpdateAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset successfully for user {UserId}", user.UserId);

        return new ResetPasswordResponse { Message = "Password has been reset successfully" };
    }

   
    private async Task<PasswordResetToken?> GetActiveResetTokenAsync(int userId, bool asTracking)
    {
        IQueryable<PasswordResetToken> query = _context.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(token => token.CreatedAt);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    
    private string GenerateOtpCode()
    {
        Span<char> buffer = _passwordResetCodeLength <= 128
            ? stackalloc char[_passwordResetCodeLength]
            : new char[_passwordResetCodeLength];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(buffer);
    }

    
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
