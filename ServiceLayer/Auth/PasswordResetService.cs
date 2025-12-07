using DataLayer.DTOs.Auth;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepositoryLayer.UnitOfWork;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly int _passwordResetCodeLength;
    private readonly int _passwordResetExpiryMinutes;

    public PasswordResetService(
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        ILogger<PasswordResetService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _logger = logger;
        _passwordResetCodeLength = Math.Clamp(configuration.GetValue("PasswordReset:CodeLength", 6), 4, 12);
        _passwordResetExpiryMinutes = Math.Clamp(configuration.GetValue("PasswordReset:CodeExpiryMinutes", 10), 1, 60);
    }


    public async Task<ForgotPasswordResponse> SendPasswordResetCodeAsync(ForgotPasswordRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _unitOfWork.Users.GetByEmailWithAccountsAsync(normalizedEmail);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("This account does not have a password set");
        }

        var otpCode = GenerateOtpCode();
        var otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_passwordResetExpiryMinutes);

        var existingTokens = await _unitOfWork.PasswordResetTokens.GetUnusedTokensByUserIdAsync(user.UserId);

        if (existingTokens.Count > 0)
        {
            _unitOfWork.PasswordResetTokens.RemoveRange(existingTokens);
        }

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            CodeHash = otpHash,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _unitOfWork.PasswordResetTokens.AddAsync(resetToken);
        await _unitOfWork.CompleteAsync();

        try
        {
            await _emailSender.SendPasswordResetCodeAsync(user.Email, user.FullName, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP to {Email}", user.Email);
            resetToken.UsedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            throw AuthServiceException.ServerError("Failed to send OTP email");
        }

        _logger.LogInformation("Password reset OTP generated for user {UserId}", user.UserId);

        return new ForgotPasswordResponse { Message = "An OTP has been sent to your email" };
    }


    public async Task<VerifyResetCodeResponse> VerifyResetCodeAsync(VerifyResetCodeRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail, asTracking: false);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var resetToken = await _unitOfWork.PasswordResetTokens.GetActiveTokenByUserIdAsync(user.UserId, asTracking: false);

        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code");
        }

        return new VerifyResetCodeResponse { Message = "OTP verified successfully" };
    }


    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _unitOfWork.Users.GetByEmailWithAccountsAsync(normalizedEmail);

        if (user == null)
        {
            throw AuthServiceException.NotFound("Email not found");
        }

        var account = user.Accounts.FirstOrDefault(a => string.IsNullOrEmpty(a.AuthProvider));
        if (account == null)
        {
            throw AuthServiceException.BadRequest("This account does not support password login");
        }

        var resetToken = await _unitOfWork.PasswordResetTokens.GetActiveTokenByUserIdAsync(user.UserId, asTracking: true);

        if (resetToken == null || !BCrypt.Net.BCrypt.Verify(request.OtpCode, resetToken.CodeHash))
        {
            throw AuthServiceException.BadRequest("Invalid or expired OTP code");
        }

        if (!string.IsNullOrEmpty(account.PasswordHash) && BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash))
        {
            throw AuthServiceException.BadRequest("New password must be different from the current password");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.UpdateAt = DateTime.UtcNow;
        resetToken.UsedAt = DateTime.UtcNow;

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Password reset successfully for user {UserId}", user.UserId);

        return new ResetPasswordResponse { Message = "Password has been reset successfully" };
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
