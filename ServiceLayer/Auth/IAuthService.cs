using DataLayer.DTOs.Auth;

namespace ServiceLayer.Auth;


public interface IAuthService
{
   
    Task<LoginResponse> LoginAsync(LoginRequestDTO request);

    
    Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);

    
    Task<SendRegistrationOtpResponse> SendRegistrationOtpAsync(SendRegistrationOtpRequest request);
    
    Task<VerifyRegistrationResponse> VerifyRegistrationAsync(VerifyRegistrationRequest request);
    
    Task<ResendOtpResponse> ResendRegistrationOtpAsync(ResendRegistrationOtpRequest request);
}