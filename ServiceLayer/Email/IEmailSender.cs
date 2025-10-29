﻿namespace ServiceLayer.Email;


public interface IEmailSender
{
    
    Task SendPasswordResetCodeAsync(string toEmail, string toName, string otpCode);
    
    Task SendRegistrationOtpAsync(string toEmail, string toName, string otpCode);
}