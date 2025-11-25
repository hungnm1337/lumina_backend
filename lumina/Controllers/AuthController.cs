using DataLayer.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Auth;

namespace lumina.Controllers;


[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;

    public AuthController(IAuthService authService, IPasswordResetService passwordResetService)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
    }

   
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        // Model validation được xử lý tự động bởi ASP.NET Core
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid login request"));
        }

        return await ExecuteAsync(() => _authService.LoginAsync(request));
    }

    
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid Google login request"));
        }

        return await ExecuteAsync(() => _authService.GoogleLoginAsync(request));
    }

    
    [HttpPost("register/send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendRegistrationOtp([FromBody] SendRegistrationOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid request"));
        }

        return await ExecuteAsync(() => _authService.SendRegistrationOtpAsync(request));
    }

    [HttpPost("register/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRegistration([FromBody] VerifyRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid verification request"));
        }

        return await ExecuteAsync(() => _authService.VerifyRegistrationAsync(request), StatusCodes.Status201Created);
    }

    [HttpPost("register/resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendRegistrationOtp([FromBody] ResendRegistrationOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid request"));
        }

        return await ExecuteAsync(() => _authService.ResendRegistrationOtpAsync(request));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid forgot password request"));
        }

        return await ExecuteAsync(() => _passwordResetService.SendPasswordResetCodeAsync(request));
    }

    
    [HttpPost("forgot-password/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyResetCodeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid verification request"));
        }

        return await ExecuteAsync(() => _passwordResetService.VerifyResetCodeAsync(request));
    }

    
    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid password reset request"));
        }

        return await ExecuteAsync(() => _passwordResetService.ResetPasswordAsync(request));
    }

    
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid refresh token request"));
        }

        return await ExecuteAsync(() => _authService.RefreshTokenAsync(request));
    }

    
    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var result = await action();

            if (successStatusCode == StatusCodes.Status200OK)
            {
                return Ok(result);
            }

            return StatusCode(successStatusCode, result);
        }
        catch (AuthServiceException ex)
        {
            return StatusCode(ex.StatusCode, new ErrorResponse(ex.Message));
        }
    }
}
