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
    Task<ForgotPasswordResponse> SendPasswordResetCodeAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken cancellationToken);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
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
        _passwordResetCodeLength = Math.Clamp(configuration.GetValue("PasswordReset:CodeLength", 6), 4, 12);
        _passwordResetExpiryMinutes = Math.Clamp(configuration.GetValue("PasswordReset:CodeExpiryMinutes", 10), 1, 60);
    }

    public async Task<ForgotPasswordResponse> SendPasswordResetCodeAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("This account does not have a password set.");
        }

        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_passwordResetExpiryMinutes);

        var existingTokens = await _context.PasswordResetTokens
            .Where(token => token.UserId == user.UserId && token.UsedAt == null)
            .ToListAsync(cancellationToken);

        if (existingTokens.Count > 0)
        {
            _context.PasswordResetTokens.RemoveRange(existingTokens);
        }

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _context.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailSender.SendPasswordResetCodeAsync(user.Email, user.FullName, otpCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email);
            resetToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            throw AuthServiceException.ServerError("Failed to send OTP email.");
        }

        _logger.LogInformation("Password reset OTP generated for user {UserId}", user.UserId);

        return new ForgotPasswordResponse { Message = "An OTP has been sent to your email" };
    }

    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var resetToken = await GetActiveResetTokenAsync(user.UserId, cancellationToken, asTracking: false);
        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code.");
        }

        return new VerifyResetCodeResponse { Message = "OTP verified successfully" };
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _context.Users
            .Include(u => u.Accounts)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null)
        {
            throw AuthServiceException.BadRequest("This account does not support password login.");
        }

        var resetToken = await GetActiveResetTokenAsync(user.UserId, cancellationToken, asTracking: true);
        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code.");
        }

        if (!string.IsNullOrEmpty(account.PasswordHash) && BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("New password must be different from the current password.");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.UpdateAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse { Message = "Password has been reset successfully" };
    }

    private async Task<PasswordResetToken?> GetActiveResetTokenAsync(int userId, CancellationToken cancellationToken, bool asTracking)
    {
        IQueryable<PasswordResetToken> query = _context.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(token => token.CreatedAt);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
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
