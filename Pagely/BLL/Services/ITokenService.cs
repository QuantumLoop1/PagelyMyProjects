using NotionClone.DAL.Entities;

namespace NotionClone.BLL.Services;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
