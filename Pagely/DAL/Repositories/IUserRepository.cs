using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByIdAsync(Guid id);
    Task AddAsync(AppUser user);
    Task SaveChangesAsync();
}
