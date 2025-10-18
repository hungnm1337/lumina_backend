using DataLayer.DTOs.Auth;

namespace ServiceLayer.Auth;


public interface IAuthService
{
   
    Task<LoginResponse> LoginAsync(LoginRequestDTO request);

    
    Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);

    
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
}