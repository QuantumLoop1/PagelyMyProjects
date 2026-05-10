using NotionClone.DAL.Entities;

namespace NotionClone.DAL.Repositories;

public interface IPageRepository
{
    Task<List<Page>> GetAllForUserAsync(Guid userId);
    Task<Page?> GetByIdAsync(Guid userId, Guid pageId);
    Task<int> GetNextOrderAsync(Guid userId, Guid? parentId);
    Task AddAsync(Page page);
    Task DeleteRangeAsync(IEnumerable<Page> pages);
    Task SaveChangesAsync();
}
