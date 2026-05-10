using NotionClone.BLL.DTOs;

namespace NotionClone.BLL.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshAsync(RefreshRequestDto request);
    Task LogoutAsync(RefreshRequestDto request);
}
