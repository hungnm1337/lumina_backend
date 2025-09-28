using System;
using DataLayer.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Auth;

namespace lumina.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
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
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid login request."));
        }

        return await ExecuteAsync(() => _authService.LoginAsync(request, cancellationToken));
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid Google login request."));
        }

        return await ExecuteAsync(() => _authService.GoogleLoginAsync(request, cancellationToken));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid registration request."));
        }

        return await ExecuteAsync(() => _authService.RegisterAsync(request, cancellationToken), StatusCodes.Status201Created);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid forgot password request."));
        }

        return await ExecuteAsync(() => _passwordResetService.SendPasswordResetCodeAsync(request, cancellationToken));
    }

    [HttpPost("forgot-password/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyResetCodeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid verification request."));
        }

        return await ExecuteAsync(() => _passwordResetService.VerifyResetCodeAsync(request, cancellationToken));
    }

    [HttpPost("forgot-password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid password reset request."));
        }

        return await ExecuteAsync(() => _passwordResetService.ResetPasswordAsync(request, cancellationToken));
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
