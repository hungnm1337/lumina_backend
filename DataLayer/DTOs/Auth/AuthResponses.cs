﻿namespace DataLayer.DTOs.Auth;

public class AuthUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public AuthUserResponse User { get; set; } = new();
}

public class SendRegistrationOtpResponse
{
    public string Message { get; set; } = string.Empty;
}

public class VerifyRegistrationResponse
{
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public AuthUserResponse User { get; set; } = new();
}

public class ResendOtpResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
}
public class VerifyResetCodeResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ResetPasswordResponse
{
    public string Message { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public ErrorResponse()
    {
    }

    public ErrorResponse(string message)
    {
        Error = message;
    }

    public string Error { get; set; } = string.Empty;
}