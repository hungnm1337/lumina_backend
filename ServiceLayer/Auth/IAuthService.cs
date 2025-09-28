using DataLayer.DTOs.Auth;

namespace ServiceLayer.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequestDTO request, CancellationToken cancellationToken);

    Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken);

    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
}