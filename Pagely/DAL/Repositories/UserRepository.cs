using Microsoft.EntityFrameworkCore;
using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AppUser?> GetByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public Task<AppUser?> GetByIdAsync(Guid id)
    {
        return _context.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task AddAsync(AppUser user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
