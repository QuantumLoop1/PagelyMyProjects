using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task AddAsync(RefreshToken token);
    Task SaveChangesAsync();
}
