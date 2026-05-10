using Microsoft.EntityFrameworkCore;
using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public sealed class PageRepository : IPageRepository
{
    private readonly AppDbContext _context;

    public PageRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Page>> GetAllForUserAsync(Guid userId)
    {
        return _context.Pages
            .Where(page => page.UserId == userId)
            .OrderBy(page => page.ParentId)
            .ThenBy(page => page.Order)
            .ThenBy(page => page.CreatedAt)
            .ToListAsync();
    }

    public Task<Page?> GetByIdAsync(Guid userId, Guid pageId)
    {
        return _context.Pages.FirstOrDefaultAsync(page => page.UserId == userId && page.Id == pageId);
    }

    public async Task<int> GetNextOrderAsync(Guid userId, Guid? parentId)
    {
        var maxOrder = await _context.Pages
            .Where(page => page.UserId == userId && page.ParentId == parentId)
            .Select(page => (int?)page.Order)
            .MaxAsync();

        return maxOrder.HasValue ? maxOrder.Value + 1 : 0;
    }

    public async Task AddAsync(Page page)
    {
        await _context.Pages.AddAsync(page);
    }

    public Task DeleteRangeAsync(IEnumerable<Page> pages)
    {
        _context.Pages.RemoveRange(pages);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
